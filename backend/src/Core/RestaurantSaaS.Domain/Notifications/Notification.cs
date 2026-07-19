using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Domain.Notifications;

public class Notification : TenantAuditableEntity
{
    public Guid? RecipientUserId { get; private set; }
    public NotificationCategory Category { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public bool IsRead { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    private Notification() { }

    public Notification(Guid tenantId, Guid? recipientUserId, NotificationCategory category, NotificationChannel channel, string title, string body)
    {
        TenantId = tenantId;
        RecipientUserId = recipientUserId;
        Category = category;
        Channel = channel;
        Title = title;
        Body = body;
    }

    public void MarkSent() => SentAt = DateTimeOffset.UtcNow;

    public void MarkRead()
    {
        IsRead = true;
        ReadAt = DateTimeOffset.UtcNow;
    }
}
