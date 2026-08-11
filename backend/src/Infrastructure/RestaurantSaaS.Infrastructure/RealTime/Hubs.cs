using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace RestaurantSaaS.Infrastructure.RealTime;

/// <summary>Kitchen Display clients connect here and join the group for their location; they receive
/// new-ticket, priority-change, and status-transition pushes the instant POS sends an order to the kitchen.</summary>
[Authorize]
public sealed class KitchenHub : Hub
{
    public const string LocationGroupPrefix = "kitchen-location-";

    public async Task JoinLocation(Guid locationId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, LocationGroupPrefix + locationId);

    public async Task LeaveLocation(Guid locationId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, LocationGroupPrefix + locationId);
}

/// <summary>Waiter/table-side clients connect here for order-ready and table-status pushes.</summary>
[Authorize]
public sealed class OrdersHub : Hub
{
    public const string LocationGroupPrefix = "orders-location-";

    public async Task JoinLocation(Guid locationId) =>
        await Groups.AddToGroupAsync(Context.ConnectionId, LocationGroupPrefix + locationId);

    public async Task LeaveLocation(Guid locationId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, LocationGroupPrefix + locationId);
}
