using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Reporting;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class DailySalesSummaryConfiguration : IEntityTypeConfiguration<DailySalesSummary>
{
    public void Configure(EntityTypeBuilder<DailySalesSummary> builder)
    {
        builder.ToTable("daily_sales_summaries");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.LocationId, x.Date }).IsUnique();
        builder.Property(x => x.GrossRevenue).HasColumnType("numeric(12,2)");
        builder.Property(x => x.NetRevenue).HasColumnType("numeric(12,2)");
        builder.Property(x => x.TaxCollected).HasColumnType("numeric(12,2)");
        builder.Property(x => x.DiscountsGiven).HasColumnType("numeric(12,2)");
        builder.Property(x => x.RefundsIssued).HasColumnType("numeric(12,2)");
        builder.Ignore(x => x.AverageOrderValue);
    }
}
