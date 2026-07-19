using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Menu;
using RestaurantSaaS.Domain.Pos;
using RestaurantSaaS.Domain.RestaurantOps;
using RestaurantSaaS.Domain.Tenancy;

namespace RestaurantSaaS.Application.Pos;

public sealed record OpenOrderCommand(Guid TenantId, Guid LocationId, Guid? TableId, Guid ServerEmployeeId, OrderSource Source)
    : IRequest<Guid>, ITenantScopedRequest;

public sealed class OpenOrderCommandHandler(IApplicationDbContext db) : IRequestHandler<OpenOrderCommand, Guid>
{
    public async Task<Guid> Handle(OpenOrderCommand request, CancellationToken ct)
    {
        var location = await db.Locations.SingleOrDefaultAsync(l => l.Id == request.LocationId && l.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Location), request.LocationId);

        var order = new Order(request.TenantId, request.LocationId, request.TableId, request.ServerEmployeeId,
            location.Currency, location.TaxConfig.DefaultTaxRatePercent, request.Source);

        db.Orders.Add(order);

        if (request.TableId is not null)
        {
            var table = await db.Tables.SingleOrDefaultAsync(t => t.Id == request.TableId && t.TenantId == request.TenantId, ct);
            table?.Occupy();
        }

        await db.SaveChangesAsync(ct);
        return order.Id;
    }
}

public sealed record OrderItemModifierSelection(Guid ModifierId);

public sealed record AddOrderItemCommand(
    Guid TenantId, Guid OrderId, Guid ProductVariantId, int Quantity, string? Notes, IReadOnlyCollection<OrderItemModifierSelection> Modifiers)
    : IRequest<Guid>, ITenantScopedRequest;

public sealed class AddOrderItemCommandValidator : AbstractValidator<AddOrderItemCommand>
{
    public AddOrderItemCommandValidator() => RuleFor(x => x.Quantity).GreaterThan(0);
}

public sealed class AddOrderItemCommandHandler(IApplicationDbContext db) : IRequestHandler<AddOrderItemCommand, Guid>
{
    public async Task<Guid> Handle(AddOrderItemCommand request, CancellationToken ct)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == request.OrderId && o.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var variant = await db.Set<ProductVariant>().SingleOrDefaultAsync(v => v.Id == request.ProductVariantId && v.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(ProductVariant), request.ProductVariantId);

        var product = await db.Products.SingleAsync(p => p.Id == variant.ProductId, ct);

        var unitPrice = Domain.ValueObjects.Money.Of(product.BasePrice.Amount + variant.PriceDelta, product.BasePrice.Currency);
        var item = order.AddItem(variant.Id, product.Name, variant.Name, unitPrice, request.Quantity, request.Notes);

        if (request.Modifiers.Count > 0)
        {
            var modifierIds = request.Modifiers.Select(m => m.ModifierId).ToList();
            var modifiers = await db.Set<Modifier>().Where(m => modifierIds.Contains(m.Id)).ToListAsync(ct);
            foreach (var modifier in modifiers)
            {
                item.AddModifier(modifier.Name, modifier.PriceDelta);
            }
        }

        await db.SaveChangesAsync(ct);
        return item.Id;
    }
}

public sealed record RemoveOrderItemCommand(Guid TenantId, Guid OrderId, Guid OrderItemId) : IRequest, ITenantScopedRequest;

public sealed class RemoveOrderItemCommandHandler(IApplicationDbContext db) : IRequestHandler<RemoveOrderItemCommand>
{
    public async Task Handle(RemoveOrderItemCommand request, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Items).SingleOrDefaultAsync(o => o.Id == request.OrderId && o.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.RemoveItem(request.OrderItemId);
        await db.SaveChangesAsync(ct);
    }
}

public sealed record ApplyDiscountCommand(Guid TenantId, Guid OrderId, DiscountType Type, decimal AmountOff, string Reason, Guid AppliedByEmployeeId)
    : IRequest, ITenantScopedRequest;

public sealed class ApplyDiscountCommandValidator : AbstractValidator<ApplyDiscountCommand>
{
    public ApplyDiscountCommandValidator() => RuleFor(x => x.AmountOff).GreaterThan(0);
}

public sealed class ApplyDiscountCommandHandler(IApplicationDbContext db) : IRequestHandler<ApplyDiscountCommand>
{
    public async Task Handle(ApplyDiscountCommand request, CancellationToken ct)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == request.OrderId && o.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.ApplyDiscount(request.Type, request.AmountOff, request.Reason, request.AppliedByEmployeeId);
        await db.SaveChangesAsync(ct);
    }
}

public sealed record AddTipCommand(Guid TenantId, Guid OrderId, decimal Amount) : IRequest, ITenantScopedRequest;

public sealed class AddTipCommandHandler(IApplicationDbContext db) : IRequestHandler<AddTipCommand>
{
    public async Task Handle(AddTipCommand request, CancellationToken ct)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == request.OrderId && o.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.AddTip(request.Amount);
        await db.SaveChangesAsync(ct);
    }
}

public sealed record SplitOrderCommand(Guid TenantId, Guid OrderId, IReadOnlyCollection<Guid> OrderItemIds, Guid RequestedByEmployeeId)
    : IRequest<Guid>, ITenantScopedRequest;

public sealed class SplitOrderCommandHandler(IApplicationDbContext db) : IRequestHandler<SplitOrderCommand, Guid>
{
    public async Task<Guid> Handle(SplitOrderCommand request, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Items).ThenInclude(i => i.Modifiers)
            .SingleOrDefaultAsync(o => o.Id == request.OrderId && o.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var newOrder = order.SplitOff(request.OrderItemIds, request.RequestedByEmployeeId);
        db.Orders.Add(newOrder);

        await db.SaveChangesAsync(ct);
        return newOrder.Id;
    }
}

public sealed record MergeOrdersCommand(Guid TenantId, Guid TargetOrderId, Guid SourceOrderId) : IRequest, ITenantScopedRequest;

public sealed class MergeOrdersCommandHandler(IApplicationDbContext db) : IRequestHandler<MergeOrdersCommand>
{
    public async Task Handle(MergeOrdersCommand request, CancellationToken ct)
    {
        var target = await db.Orders.Include(o => o.Items).ThenInclude(i => i.Modifiers)
            .SingleOrDefaultAsync(o => o.Id == request.TargetOrderId && o.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Order), request.TargetOrderId);

        var source = await db.Orders.Include(o => o.Items).ThenInclude(i => i.Modifiers)
            .SingleOrDefaultAsync(o => o.Id == request.SourceOrderId && o.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Order), request.SourceOrderId);

        target.MergeFrom(source);
        await db.SaveChangesAsync(ct);
    }
}
