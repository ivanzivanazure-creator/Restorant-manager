using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Pos;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.LocationId, x.Status });
        builder.HasIndex(x => x.TableId);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.TaxRatePercent).HasColumnType("numeric(5,2)");
        builder.Property(x => x.TipAmount).HasColumnType("numeric(10,2)");

        // Computed monetary totals are derived in-memory from Items/Discounts/Payments — not persisted columns.
        builder.Ignore(x => x.Subtotal);
        builder.Ignore(x => x.DiscountTotal);
        builder.Ignore(x => x.TaxableAmount);
        builder.Ignore(x => x.TaxTotal);
        builder.Ignore(x => x.GrandTotal);
        builder.Ignore(x => x.AmountPaid);
        builder.Ignore(x => x.AmountDue);

        builder.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Payments).WithOne().HasForeignKey(p => p.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Discounts).WithOne().HasForeignKey(d => d.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OrderId);
        builder.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.VariantName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Ignore(x => x.LineTotal);

        builder.OwnsOne(x => x.UnitPrice, m =>
        {
            m.Property(p => p.Amount).HasColumnName("unit_price_amount").HasColumnType("numeric(10,2)");
            m.Property(p => p.Currency).HasColumnName("unit_price_currency").HasMaxLength(3);
        });

        builder.HasMany(x => x.Modifiers).WithOne().HasForeignKey(m => m.OrderItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderItemModifierConfiguration : IEntityTypeConfiguration<OrderItemModifier>
{
    public void Configure(EntityTypeBuilder<OrderItemModifier> builder)
    {
        builder.ToTable("order_item_modifiers");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OrderItemId);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PriceDelta).HasColumnType("numeric(10,2)");
    }
}

public class DiscountApplicationConfiguration : IEntityTypeConfiguration<DiscountApplication>
{
    public void Configure(EntityTypeBuilder<DiscountApplication> builder)
    {
        builder.ToTable("discount_applications");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OrderId);
        builder.Property(x => x.Reason).HasMaxLength(300);
        builder.Property(x => x.AmountOff).HasColumnType("numeric(10,2)");
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OrderId);
        builder.Property(x => x.Amount).HasColumnType("numeric(10,2)");
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.Reference).HasMaxLength(200);
    }
}

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("refunds");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OrderId);
        builder.Property(x => x.Amount).HasColumnType("numeric(10,2)");
        builder.Property(x => x.Reason).HasMaxLength(300);
    }
}
