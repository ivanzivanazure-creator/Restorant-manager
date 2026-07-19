using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSaaS.Domain.Inventory;

namespace RestaurantSaaS.Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouses");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.LocationId);
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
    }
}

public class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.ToTable("ingredients");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Barcode).HasMaxLength(64);
        builder.Property(x => x.ReorderThreshold).HasColumnType("numeric(12,3)");
        builder.Property(x => x.CostPerUnit).HasColumnType("numeric(12,4)");
    }
}

public class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        builder.ToTable("stock_levels");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.WarehouseId, x.IngredientId }).IsUnique();
        builder.Property(x => x.QuantityOnHand).HasColumnType("numeric(12,3)");

        builder.HasMany(x => x.Batches).WithOne().HasForeignKey(b => b.StockLevelId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class StockBatchConfiguration : IEntityTypeConfiguration<StockBatch>
{
    public void Configure(EntityTypeBuilder<StockBatch> builder)
    {
        builder.ToTable("stock_batches");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.StockLevelId);
        builder.HasIndex(x => x.ExpiresAt);
        builder.Property(x => x.Quantity).HasColumnType("numeric(12,3)");
        builder.Property(x => x.RemainingQuantity).HasColumnType("numeric(12,3)");
        builder.Property(x => x.UnitCost).HasColumnType("numeric(12,4)");
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("stock_movements");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.WarehouseId, x.IngredientId });
        builder.Property(x => x.Quantity).HasColumnType("numeric(12,3)");
        builder.Property(x => x.Reference).HasMaxLength(200);
    }
}

public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        builder.ToTable("stock_transfers");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.Quantity).HasColumnType("numeric(12,3)");
    }
}

public class WasteRecordConfiguration : IEntityTypeConfiguration<WasteRecord>
{
    public void Configure(EntityTypeBuilder<WasteRecord> builder)
    {
        builder.ToTable("waste_records");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TenantId);
        builder.Property(x => x.Quantity).HasColumnType("numeric(12,3)");
        builder.Property(x => x.Reason).HasMaxLength(200);
    }
}
