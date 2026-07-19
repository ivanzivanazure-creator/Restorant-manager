using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Procurement;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContactEmail).HasMaxLength(256);
        builder.HasMany(x => x.PriceLists).WithOne().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.ToTable("price_lists");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.SupplierId);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.PriceListId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> builder)
    {
        builder.ToTable("price_list_items");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PriceListId);
        builder.Property(x => x.UnitPrice).HasColumnType("numeric(12,4)");
        builder.Property(x => x.DiscountPercent).HasColumnType("numeric(5,2)");
        builder.Ignore(x => x.NetUnitPrice);
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.SupplierId, x.Status });
        builder.Ignore(x => x.TotalAmount);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("purchase_order_lines");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PurchaseOrderId);
        builder.Property(x => x.Quantity).HasColumnType("numeric(12,3)");
        builder.Property(x => x.UnitPrice).HasColumnType("numeric(12,4)");
        builder.Property(x => x.ReceivedQuantity).HasColumnType("numeric(12,3)");
        builder.Ignore(x => x.IsFullyReceived);
    }
}

public class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
{
    public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
    {
        builder.ToTable("goods_receipts");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PurchaseOrderId);
        builder.Property(x => x.Notes).HasMaxLength(1000);
    }
}
