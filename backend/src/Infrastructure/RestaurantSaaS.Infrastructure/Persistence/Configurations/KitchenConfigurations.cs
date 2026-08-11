using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Kitchen;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class KitchenTicketConfiguration : IEntityTypeConfiguration<KitchenTicket>
{
    public void Configure(EntityTypeBuilder<KitchenTicket> builder)
    {
        builder.ToTable("kitchen_tickets");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.LocationId, x.Status });
        builder.Property(x => x.TableLabel).HasMaxLength(50);
        builder.Ignore(x => x.ElapsedCookTime);
        builder.Ignore(x => x.IsOverdue);

        builder.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.KitchenTicketId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class KitchenTicketItemConfiguration : IEntityTypeConfiguration<KitchenTicketItem>
{
    public void Configure(EntityTypeBuilder<KitchenTicketItem> builder)
    {
        builder.ToTable("kitchen_ticket_items");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.KitchenTicketId);
        builder.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.VariantName).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(500);
    }
}
