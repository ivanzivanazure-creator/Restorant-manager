using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Audit;
using RestaurantSaaS.Domain.Notifications;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.RecipientUserId, x.IsRead });
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => x.OccurredAt);
        builder.Property(x => x.Action).HasMaxLength(150).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.Metadata).HasColumnType("jsonb");
    }
}
