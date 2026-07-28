using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Domain.Integrations;

/// <summary>A tenant's connection to one third-party delivery platform for one location. The platform
/// posts orders to POST /api/v1/integrations/delivery/{platform}/webhook/{locationId}, authenticated by
/// a shared secret (not a JWT — the caller is UberEats/DoorDash, not a logged-in staff member) verified
/// against WebhookSecretHash. See Application/Integrations/DeliveryOrderIngestion.cs.</summary>
public class DeliveryIntegration : TenantAuditableEntity
{
    public Guid LocationId { get; private set; }
    public DeliveryPlatform Platform { get; private set; }
    public string WebhookSecretHash { get; private set; } = default!;
    public string? ExternalStoreId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? LastOrderReceivedAt { get; private set; }

    private DeliveryIntegration() { }

    public DeliveryIntegration(Guid tenantId, Guid locationId, DeliveryPlatform platform, string webhookSecretHash, string? externalStoreId)
    {
        TenantId = tenantId;
        LocationId = locationId;
        Platform = platform;
        WebhookSecretHash = webhookSecretHash;
        ExternalStoreId = externalStoreId;
    }

    public void RecordOrderReceived() => LastOrderReceivedAt = DateTimeOffset.UtcNow;
    public void Deactivate() => IsActive = false;
    public void Reactivate() => IsActive = true;
    public void RotateSecret(string newWebhookSecretHash) => WebhookSecretHash = newWebhookSecretHash;
}
