using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Pos;

namespace RestaurantSaaS.Application.Pos;

public sealed record GetOrderQuery(Guid TenantId, Guid OrderId) : IRequest<OrderDto>, ITenantScopedRequest;

public sealed class GetOrderQueryHandler(IApplicationDbContext db) : IRequestHandler<GetOrderQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Modifiers)
            .Include(o => o.Payments)
            .SingleOrDefaultAsync(o => o.Id == request.OrderId && o.TenantId == request.TenantId, ct)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        return OrderMapper.ToDto(order);
    }
}

public sealed record GetOpenOrdersByLocationQuery(Guid TenantId, Guid LocationId) : IRequest<IReadOnlyCollection<OrderDto>>, ITenantScopedRequest;

public sealed class GetOpenOrdersByLocationQueryHandler(IApplicationDbContext db) : IRequestHandler<GetOpenOrdersByLocationQuery, IReadOnlyCollection<OrderDto>>
{
    private static readonly OrderStatus[] OpenStatuses = [OrderStatus.Open, OrderStatus.InKitchen, OrderStatus.ReadyToServe, OrderStatus.Served, OrderStatus.PartiallyPaid];

    public async Task<IReadOnlyCollection<OrderDto>> Handle(GetOpenOrdersByLocationQuery request, CancellationToken ct)
    {
        var orders = await db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Modifiers)
            .Include(o => o.Payments)
            .Where(o => o.LocationId == request.LocationId && o.TenantId == request.TenantId && OpenStatuses.Contains(o.Status))
            .OrderBy(o => o.OpenedAt)
            .ToListAsync(ct);

        return orders.Select(OrderMapper.ToDto).ToList();
    }
}

internal static class OrderMapper
{
    public static OrderDto ToDto(Order order) => new(
        order.Id, order.LocationId, order.TableId, order.Status, order.Source, order.Currency,
        order.Subtotal, order.DiscountTotal, order.TaxTotal, order.TipAmount, order.GrandTotal,
        order.AmountPaid, order.AmountDue, order.OpenedAt,
        order.Items.Select(i => new OrderItemDto(i.Id, i.ProductVariantId, i.ProductName, i.VariantName,
            i.UnitPrice.Amount, i.Quantity, i.Notes, i.LineTotal,
            i.Modifiers.Select(m => new OrderItemModifierDto(m.Name, m.PriceDelta)).ToList())).ToList(),
        order.Payments.Select(p => new PaymentDto(p.Id, p.Method, p.Amount, p.Status, p.Reference)).ToList());
}
