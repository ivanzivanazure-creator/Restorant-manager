using Microsoft.AspNetCore.SignalR;
using RestaurantSaaS.Application.Common.Interfaces;

namespace RestaurantSaaS.Infrastructure.RealTime;

public sealed class SignalRRealtimeNotifier(IHubContext<KitchenHub> kitchenHub, IHubContext<OrdersHub> ordersHub) : IRealtimeNotifier
{
    public Task NotifyKitchenAsync(Guid locationId, object payload, CancellationToken ct = default) =>
        kitchenHub.Clients.Group(KitchenHub.LocationGroupPrefix + locationId).SendAsync("kitchenEvent", payload, ct);

    public Task NotifyOrdersAsync(Guid locationId, object payload, CancellationToken ct = default) =>
        ordersHub.Clients.Group(OrdersHub.LocationGroupPrefix + locationId).SendAsync("orderEvent", payload, ct);
}
