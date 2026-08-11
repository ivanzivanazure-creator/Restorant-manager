using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Billing;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Pos;
using RestaurantSaaS.Domain.Subscription;

namespace RestaurantSaaS.Application.Pos;

/// <summary>Sends the order's current items to the kitchen: creates the KitchenTicket, flips the order
/// to InKitchen, and — per the required workflow ("Chef sees recipe... Inventory decreases automatically")
/// — deducts each line's recipe ingredients from the location's stock via FIFO consumption.</summary>
public sealed record SendOrderToKitchenCommand(Guid TenantId, Guid OrderId, Guid WarehouseId, int TargetCookMinutes)
    : IRequest<Guid>, ITenantScopedRequest;

public sealed class SendOrderToKitchenCommandHandler(IApplicationDbContext db, OrderKitchenDispatchService dispatchService)
    : IRequestHandler<SendOrderToKitchenCommand, Guid>
{
    public async Task<Guid> Handle(SendOrderToKitchenCommand request, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Items).ThenInclude(i => i.Modifiers)
            .SingleOrDefaultAsync(o => o.Id == request.OrderId && o.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var ticket = await dispatchService.DispatchAsync(order, request.WarehouseId, request.TargetCookMinutes, ct);
        return ticket.Id;
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

        await RecordPlatformFeeIfApplicableAsync(order, payment, ct);

        await db.SaveChangesAsync(ct);

        await realtime.NotifyOrdersAsync(order.LocationId, new { @event = "order-paid", orderId = order.Id, order.Status }, ct);

        return new PaymentDto(payment.Id, payment.Method, payment.Amount, payment.Status, payment.Reference);
    }

    /// <summary>Usage-based revenue: card-rail payments (Card/MobileWallet) for tenants on a package with
    /// a non-zero TransactionFeePercent generate a PlatformFeeLedgerEntry — the accounting record behind
    /// the platform's take-rate. Cash/Voucher/RoomCharge never carry a platform fee since no card network
    /// touched them. See docs/ARCHITECTURE.md "Billing & platform fees".</summary>
    private async Task RecordPlatformFeeIfApplicableAsync(Order order, Payment payment, CancellationToken ct)
    {
        if (payment.Method is not (PaymentMethod.Card or PaymentMethod.MobileWallet)) return;

        var subscription = await db.Set<TenantSubscription>().SingleOrDefaultAsync(s => s.TenantId == order.TenantId, ct);
        if (subscription is null) return;

        var package = await db.Packages.SingleOrDefaultAsync(p => p.Id == subscription.PackageId, ct);
        if (package is null || package.TransactionFeePercent <= 0) return;

        db.PlatformFeeLedgerEntries.Add(new PlatformFeeLedgerEntry(
            order.TenantId, payment.Id, order.Id, payment.Amount, package.TransactionFeePercent, payment.Currency));
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
