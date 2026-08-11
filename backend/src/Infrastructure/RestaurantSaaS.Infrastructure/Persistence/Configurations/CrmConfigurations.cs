using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Crm;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("crm_customers");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.HasIndex(x => new { x.TenantId, x.Email });
    }
}

public class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.ToTable("crm_loyalty_accounts");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CustomerId).IsUnique();
        builder.Property(x => x.TierName).HasMaxLength(50);
        builder.HasMany(x => x.Transactions).WithOne().HasForeignKey(t => t.LoyaltyAccountId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.ToTable("crm_loyalty_transactions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.LoyaltyAccountId);
        builder.Property(x => x.Reason).HasMaxLength(200);
    }
}

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("crm_coupons");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        builder.Property(x => x.DiscountPercent).HasColumnType("numeric(5,2)");
    }
}

public class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> builder)
    {
        builder.ToTable("crm_coupon_redemptions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CouponId);
        builder.HasIndex(x => x.OrderId);
    }
}

public class GiftCardConfiguration : IEntityTypeConfiguration<GiftCard>
{
    public void Configure(EntityTypeBuilder<GiftCard> builder)
    {
        builder.ToTable("crm_gift_cards");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.InitialBalance).HasColumnType("numeric(10,2)");
        builder.Property(x => x.CurrentBalance).HasColumnType("numeric(10,2)");
    }
}

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("crm_feedback");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.Comment).HasMaxLength(2000);
    }
}
