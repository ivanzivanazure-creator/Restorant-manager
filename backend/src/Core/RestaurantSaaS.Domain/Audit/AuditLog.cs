using RestaurantSaaS.Domain.Common;

namespace RestaurantSaaS.Domain.Audit;

/// <summary>Immutable record of a sensitive action, e.g. subscription change, refund, inventory correction,
/// role/permission change. Written by an Application-layer pipeline behavior, never mutated afterwards.</summary>
public class AuditLog : BaseEntity
{
    public Guid? TenantId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; } = default!; // e.g. "Order.Refund", "Subscription.Activate"
    public string EntityType { get; private set; } = default!;
    public string EntityId { get; private set; } = default!;
    public string? Metadata { get; private set; } // JSON blob of before/after or relevant details
    public string IpAddress { get; private set; } = default!;
    public DateTimeOffset OccurredAt { get; private set; }

    public AuditLog(Guid? tenantId, Guid actorUserId, string action, string entityType, string entityId, string? metadata, string ipAddress)
    {
        TenantId = tenantId;
        ActorUserId = actorUserId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Metadata = metadata;
        IpAddress = ipAddress;
        OccurredAt = DateTimeOffset.UtcNow;
    }
}
