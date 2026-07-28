using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Subscription;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.ToTable("packages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.MonthlyPrice).HasColumnType("numeric(10,2)");
        builder.Property(x => x.YearlyPrice).HasColumnType("numeric(10,2)");
        builder.Property(x => x.TransactionFeePercent).HasColumnType("numeric(5,2)");
        builder.Property(x => x.SlaUptimeTargetPercent).HasColumnType("numeric(5,2)");
        builder.Property(x => x.FeatureFlags)
            .HasConversion(JsonValueConverter<IReadOnlyDictionary<string, bool>>.Converter, JsonValueConverter<IReadOnlyDictionary<string, bool>>.Comparer)
            .HasColumnType("jsonb");
    }
}

public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.ToTable("tenant_subscriptions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.Property(x => x.StripeCustomerId).HasMaxLength(100);
        builder.Property(x => x.StripeSubscriptionId).HasMaxLength(100);
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantSubscriptionId);
        builder.Property(x => x.Number).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();
        builder.Property(x => x.Amount).HasColumnType("numeric(10,2)");
        builder.Property(x => x.Currency).HasMaxLength(3);
    }
}
