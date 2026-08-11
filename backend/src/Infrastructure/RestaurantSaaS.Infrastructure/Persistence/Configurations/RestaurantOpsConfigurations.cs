using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.RestaurantOps;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class TableConfiguration : IEntityTypeConfiguration<Table>
{
    public void Configure(EntityTypeBuilder<Table> builder)
    {
        builder.ToTable("tables");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.LocationId);
        builder.Property(x => x.Label).HasMaxLength(50).IsRequired();

        builder.HasOne(x => x.QrCode).WithOne().HasForeignKey<QrCode>(q => q.TableId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class QrCodeConfiguration : IEntityTypeConfiguration<QrCode>
{
    public void Configure(EntityTypeBuilder<QrCode> builder)
    {
        builder.ToTable("qr_codes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Token).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.Token).IsUnique();
    }
}
