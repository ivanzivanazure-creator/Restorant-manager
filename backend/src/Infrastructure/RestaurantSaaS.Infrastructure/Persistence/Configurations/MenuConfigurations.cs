using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Menu;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        builder.ToTable("menu_categories");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.LocationId);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.HasMany(x => x.Products).WithOne().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CategoryId);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.OwnsOne(x => x.BasePrice, m =>
        {
            m.Property(p => p.Amount).HasColumnName("base_price_amount").HasColumnType("numeric(10,2)");
            m.Property(p => p.Currency).HasColumnName("base_price_currency").HasMaxLength(3);
        });

        builder.Property(x => x.Allergens)
            .HasConversion(JsonValueConverter<IReadOnlyCollection<Domain.Enums.Allergen>>.Converter, JsonValueConverter<IReadOnlyCollection<Domain.Enums.Allergen>>.Comparer)
            .HasColumnType("jsonb");

        builder.HasMany(x => x.Variants).WithOne().HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ModifierGroups).WithOne().HasForeignKey(g => g.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.AvailabilityRules).AutoInclude(false);
        builder.HasMany(x => x.AvailabilityRules).WithOne().HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ProductId);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PriceDelta).HasColumnType("numeric(10,2)");
    }
}

public class ProductModifierGroupConfiguration : IEntityTypeConfiguration<ProductModifierGroup>
{
    public void Configure(EntityTypeBuilder<ProductModifierGroup> builder)
    {
        builder.ToTable("product_modifier_groups");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ProductId);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.HasMany(x => x.Modifiers).WithOne().HasForeignKey(m => m.ModifierGroupId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ModifierConfiguration : IEntityTypeConfiguration<Modifier>
{
    public void Configure(EntityTypeBuilder<Modifier> builder)
    {
        builder.ToTable("modifiers");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ModifierGroupId);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PriceDelta).HasColumnType("numeric(10,2)");
    }
}

public class MenuAvailabilityRuleConfiguration : IEntityTypeConfiguration<MenuAvailabilityRule>
{
    public void Configure(EntityTypeBuilder<MenuAvailabilityRule> builder)
    {
        builder.ToTable("menu_availability_rules");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ProductId);

        builder.OwnsOne(x => x.Hours, h =>
        {
            h.Property(p => p.Start).HasColumnName("start_time");
            h.Property(p => p.End).HasColumnName("end_time");
        });
    }
}
