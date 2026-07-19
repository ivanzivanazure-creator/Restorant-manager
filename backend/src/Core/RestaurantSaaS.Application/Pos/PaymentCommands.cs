using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Inventory;
using RestaurantSaaS.Domain.Kitchen;
using RestaurantSaaS.Domain.Menu;
using RestaurantSaaS.Domain.Pos;
using RestaurantSaaS.Domain.Recipes;

namespace RestaurantSaaS.Application.Pos;

/// <summary>Sends the order's current items to the kitchen: creates the KitchenTicket, flips the order
/// to InKitchen, and — per the required workflow ("Chef sees recipe... Inventory decreases automatically")
/// — deducts each line's recipe ingredients from the location's stock via FIFO consumption.</summary>
public sealed record SendOrderToKitchenCommand(Guid TenantId, Guid OrderId, Guid WarehouseId, int TargetCookMinutes)
    : IRequest<Guid>, ITenantScopedRequest;

public sealed class SendOrderToKitchenCommandHandler(IApplicationDbContext db, IRealtimeNotifier realtime)
    : IRequestHandler<SendOrderToKitchenCommand, Guid>
{
    public async Task<Guid> Handle(SendOrderToKitchenCommand request, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Items).ThenInclude(i => i.Modifiers)
            .SingleOrDefaultAsync(o => o.Id == request.OrderId && o.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        string? tableLabel = null;
        if (order.TableId is not null)
        {
            tableLabel = (await db.Tables.SingleOrDefaultAsync(t => t.Id == order.TableId, ct))?.Label;
        }

        var ticket = new KitchenTicket(request.TenantId, order.Id, order.LocationId, tableLabel, request.TargetCookMinutes);
        foreach (var item in order.Items)
        {
            ticket.AddItem(item.Id, item.ProductName, item.VariantName, item.Quantity, item.Notes);
        }
        db.Set<KitchenTicket>().Add(ticket);

        await DeductInventoryAsync(order, request.WarehouseId, ct);

        order.SendToKitchen();
        await db.SaveChangesAsync(ct);

        await realtime.NotifyKitchenAsync(order.LocationId, new
        {
            @event = "ticket-queued",
            ticketId = ticket.Id,
            orderId = order.Id,
            tableLabel,
            items = ticket.Items.Select(i => new { i.ProductName, i.VariantName, i.Quantity, i.Notes }),
        }, ct);

        return ticket.Id;
    }

    private async Task DeductInventoryAsync(Order order, Guid warehouseId, CancellationToken ct)
    {
        foreach (var item in order.Items)
        {
            var variant = await db.Set<ProductVariant>().SingleAsync(v => v.Id == item.ProductVariantId, ct);
            var recipe = await db.Set<Recipe>().Include(r => r.Ingredients)
                .SingleOrDefaultAsync(r => r.ProductId == variant.ProductId, ct);
            if (recipe is null) continue; // no recipe modeled yet for this product — nothing to deduct

            foreach (var line in recipe.Ingredients)
            {
                var stock = await db.Set<StockLevel>().Include(s => s.Batches)
                    .SingleOrDefaultAsync(s => s.WarehouseId == warehouseId && s.IngredientId == line.IngredientId, ct);
                if (stock is null) continue;

                var ingredientName = (await db.Ingredients.SingleAsync(i => i.Id == line.IngredientId, ct)).Name;
                stock.Consume(line.Quantity * item.Quantity, ingredientName);

                db.StockMovements.Add(new Domain.Inventory.StockMovement(
                    order.TenantId, warehouseId, line.IngredientId, StockMovementType.Sale,
                    -(line.Quantity * item.Quantity), reference: order.Id.ToString(), performedByEmployeeId: order.ServerEmployeeId));
            }
        }
    }
}

public sealed record PayOrderCommand(Guid TenantId, Guid OrderId, PaymentMethod Method, decimal Amount, string? Reference)
    : IRequest<PaymentDto>, ITenantScopedRequest;

public sealed class PayOrderCommandValidator : AbstractValidator<PayOrderCommand>
{
    public PayOrderCommandValidator() => RuleFor(x => x.Amount).GreaterThan(0);
}

public sealed class PayOrderCommandHandler(IApplicationDbContext db, IRealtimeNotifier realtime) : IRequestHandler<PayOrderCommand, PaymentDto>
{
    public async Task<PaymentDto> Handle(PayOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Items).Include(o => o.Payments).Include(o => o.Discounts)
            .SingleOrDefaultAsync(o => o.Id == request.OrderId && o.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var payment = order.Pay(request.Method, request.Amount, request.Reference);

        if (order.Status == OrderStatus.Paid && order.TableId is not null)
        {
            var table = await db.Tables.SingleOrDefaultAsync(t => t.Id == order.TableId, ct);
            table?.SetCleaning();
        }

        await db.SaveChangesAsync(ct);

        await realtime.NotifyOrdersAsync(order.LocationId, new { @event = "order-paid", orderId = order.Id, order.Status }, ct);

        return new PaymentDto(payment.Id, payment.Method, payment.Amount, payment.Status, payment.Reference);
    }
}

public sealed record RefundOrderCommand(Guid TenantId, Guid OrderId, decimal Amount, string Reason, Guid IssuedByEmployeeId)
    : IRequest, ITenantScopedRequest;

public sealed class RefundOrderCommandValidator : AbstractValidator<RefundOrderCommand>
{
    public RefundOrderCommandValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(300);
    }
}

public sealed class RefundOrderCommandHandler(IApplicationDbContext db) : IRequestHandler<RefundOrderCommand>
{
    public async Task Handle(RefundOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Payments)
            .SingleOrDefaultAsync(o => o.Id == request.OrderId && o.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var refund = order.IssueRefund(request.Amount, request.Reason, request.IssuedByEmployeeId);
        db.Set<Refund>().Add(refund);

        await db.SaveChangesAsync(ct);
    }
}

public sealed record MarkOrderServedCommand(Guid TenantId, Guid OrderId) : IRequest, ITenantScopedRequest;

public sealed class MarkOrderServedCommandHandler(IApplicationDbContext db) : IRequestHandler<MarkOrderServedCommand>
{
    public async Task Handle(MarkOrderServedCommand request, CancellationToken ct)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == request.OrderId && o.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.MarkServed();
        await db.SaveChangesAsync(ct);
    }
}
