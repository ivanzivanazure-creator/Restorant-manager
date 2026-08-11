using MediatR;
using RestaurantSaaS.Application.Common.Events;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Pos;

namespace RestaurantSaaS.Application.Pos.EventHandlers;

/// <summary>Translates Order aggregate events into real-time pushes for waiter-facing clients (OrdersHub).
/// Runs after the triggering SaveChanges commits — see DomainEventDispatchInterceptor.</summary>
public sealed class OrderReadyToServeRealtimeHandler(IRealtimeNotifier realtime)
    : INotificationHandler<DomainEventNotification<OrderReadyToServeEvent>>
{
    public Task Handle(DomainEventNotification<OrderReadyToServeEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        return realtime.NotifyOrdersAsync(e.LocationId, new { @event = "order-ready", orderId = e.OrderId, tableId = e.TableId }, ct);
    }
}

public sealed class OrderPaidRealtimeHandler(IRealtimeNotifier realtime)
    : INotificationHandler<DomainEventNotification<OrderPaidEvent>>
{
    public Task Handle(DomainEventNotification<OrderPaidEvent> notification, CancellationToken ct)
    {
        var e = notification.DomainEvent;
        return realtime.NotifyOrdersAsync(e.LocationId, new { @event = "order-paid", orderId = e.OrderId }, ct);
    }
}
