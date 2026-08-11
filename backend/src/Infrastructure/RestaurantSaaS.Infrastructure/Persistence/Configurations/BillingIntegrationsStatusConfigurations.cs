using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Billing;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Integrations;
using RestaurantSaaS.Domain.Status;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class PlatformFeeLedgerEntryConfiguration : IEntityTypeConfiguration<PlatformFeeLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PlatformFeeLedgerEntry> builder)
    {
        builder.ToTable("platform_fee_ledger_entries");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.PaymentId).IsUnique();
        builder.Property(x => x.PaymentAmount).HasColumnType("numeric(10,2)");
        builder.Property(x => x.FeeRatePercent).HasColumnType("numeric(5,2)");
        builder.Property(x => x.FeeAmount).HasColumnType("numeric(10,2)");
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.StripeApplicationFeeId).HasMaxLength(100);
    }
}

public class DeliveryIntegrationConfiguration : IEntityTypeConfiguration<DeliveryIntegration>
{
    public void Configure(EntityTypeBuilder<DeliveryIntegration> builder)
    {
        builder.ToTable("delivery_integrations");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.LocationId, x.Platform }).IsUnique();
        builder.Property(x => x.WebhookSecretHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ExternalStoreId).HasMaxLength(100);
    }
}

public class SystemIncidentConfiguration : IEntityTypeConfiguration<SystemIncident>
{
    public void Configure(EntityTypeBuilder<SystemIncident> builder)
    {
        builder.ToTable("system_incidents");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.StartedAt);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();

        builder.Property(x => x.AffectedComponents)
            .HasConversion(JsonValueConverter<IReadOnlyCollection<PlatformComponent>>.Converter, JsonValueConverter<IReadOnlyCollection<PlatformComponent>>.Comparer)
            .HasColumnType("jsonb");

        builder.HasMany(x => x.Updates).WithOne().HasForeignKey(u => u.IncidentId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class IncidentUpdateConfiguration : IEntityTypeConfiguration<IncidentUpdate>
{
    public void Configure(EntityTypeBuilder<IncidentUpdate> builder)
    {
        builder.ToTable("system_incident_updates");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.IncidentId);
        builder.Property(x => x.Message).HasMaxLength(4000).IsRequired();
    }
}
