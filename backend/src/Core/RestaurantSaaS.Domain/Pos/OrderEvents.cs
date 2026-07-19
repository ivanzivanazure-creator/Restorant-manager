using RestaurantSaaS.Domain.Common;

namespace RestaurantSaaS.Domain.Pos;

public sealed class OrderPlacedEvent(Guid orderId, Guid tenantId, Guid locationId) : BaseDomainEvent
{
    public Guid OrderId { get; } = orderId;
    public Guid TenantId { get; } = tenantId;
    public Guid LocationId { get; } = locationId;
}

public sealed class OrderItemsAddedEvent(Guid orderId, Guid tenantId, Guid locationId, IReadOnlyCollection<Guid> orderItemIds) : BaseDomainEvent
{
    public Guid OrderId { get; } = orderId;
    public Guid TenantId { get; } = tenantId;
    public Guid LocationId { get; } = locationId;
    public IReadOnlyCollection<Guid> OrderItemIds { get; } = orderItemIds;
}

public sealed class OrderReadyToServeEvent(Guid orderId, Guid tenantId, Guid locationId, Guid? tableId) : BaseDomainEvent
{
    public Guid OrderId { get; } = orderId;
    public Guid TenantId { get; } = tenantId;
    public Guid LocationId { get; } = locationId;
    public Guid? TableId { get; } = tableId;
}

public sealed class OrderPaidEvent(Guid orderId, Guid tenantId, Guid locationId) : BaseDomainEvent
{
    public Guid OrderId { get; } = orderId;
    public Guid TenantId { get; } = tenantId;
    public Guid LocationId { get; } = locationId;
}
