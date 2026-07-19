using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Recipes;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipes");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ProductId).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Ignore(x => x.TotalMinutes);

        builder.HasMany(x => x.Ingredients).WithOne().HasForeignKey(i => i.RecipeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Steps).WithOne().HasForeignKey(s => s.RecipeId).OnDelete(DeleteBehavior.Cascade);

        // Versioning snapshots (ingredient/step history) are Phase 2: persisted append-only once the
        // Recipes module gets its Application/API layer. Excluded from the model for now so the tuple-based
        // in-memory snapshot (RecipeVersion) doesn't force a premature schema decision.
        builder.Ignore(x => x.Versions);
    }
}

public class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("recipe_ingredients");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.RecipeId);
        builder.Property(x => x.Quantity).HasColumnType("numeric(12,3)");
        builder.Property(x => x.Unit).HasMaxLength(20);
    }
}

public class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStep>
{
    public void Configure(EntityTypeBuilder<RecipeStep> builder)
    {
        builder.ToTable("recipe_steps");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.RecipeId);
        builder.Property(x => x.Instruction).HasMaxLength(2000).IsRequired();
    }
}
