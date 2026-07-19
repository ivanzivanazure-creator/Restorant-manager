using RestaurantSaaS.Domain.Common;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Exceptions;

namespace RestaurantSaaS.Domain.Inventory;

public class Warehouse : TenantAuditableEntity
{
    public Guid LocationId { get; private set; }
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private Warehouse() { }

    public Warehouse(Guid tenantId, Guid locationId, string name)
    {
        TenantId = tenantId;
        LocationId = locationId;
        Name = name;
    }

    public void Deactivate() => IsActive = false;
}

public class Ingredient : TenantAuditableEntity
{
    public string Name { get; private set; } = default!;
    public MeasurementUnit Unit { get; private set; }
    public decimal ReorderThreshold { get; private set; }
    public decimal CostPerUnit { get; private set; }
    public string? Barcode { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Ingredient() { }

    public Ingredient(Guid tenantId, string name, MeasurementUnit unit, decimal reorderThreshold, decimal costPerUnit, string? barcode = null)
    {
        TenantId = tenantId;
        Name = name;
        Unit = unit;
        ReorderThreshold = reorderThreshold;
        CostPerUnit = costPerUnit;
        Barcode = barcode;
    }

    public void UpdateCost(decimal costPerUnit) => CostPerUnit = costPerUnit;
    public void UpdateReorderThreshold(decimal threshold) => ReorderThreshold = threshold;
    public void Deactivate() => IsActive = false;
}

/// <summary>Aggregate root for one ingredient's stock in one warehouse — owns FIFO batches.</summary>
public class StockLevel : TenantAuditableEntity
{
    public Guid WarehouseId { get; private set; }
    public Guid IngredientId { get; private set; }
    public decimal QuantityOnHand { get; private set; }

    private readonly List<StockBatch> _batches = [];
    public IReadOnlyCollection<StockBatch> Batches => _batches.AsReadOnly();

    private StockLevel() { }

    public StockLevel(Guid tenantId, Guid warehouseId, Guid ingredientId)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        IngredientId = ingredientId;
        QuantityOnHand = 0;
    }

    public StockBatch Receive(decimal quantity, DateTimeOffset? expiresAt, decimal unitCost)
    {
        if (quantity <= 0) throw new DomainException("Received quantity must be positive.");
        var batch = new StockBatch(Id, quantity, DateTimeOffset.UtcNow, expiresAt, unitCost);
        _batches.Add(batch);
        QuantityOnHand += quantity;
        return batch;
    }

    /// <summary>FIFO consumption: depletes oldest (by ReceivedAt) batches first; earliest-expiring ties broken by expiry.</summary>
    public IReadOnlyCollection<(StockBatch Batch, decimal QuantityTaken)> Consume(decimal quantity, string ingredientName)
    {
        if (quantity <= 0) throw new DomainException("Consumption quantity must be positive.");
        if (quantity > QuantityOnHand) throw new InsufficientStockException(ingredientName, quantity, QuantityOnHand);

        var taken = new List<(StockBatch, decimal)>();
        var remaining = quantity;

        foreach (var batch in _batches.Where(b => b.RemainingQuantity > 0)
                     .OrderBy(b => b.ExpiresAt ?? DateTimeOffset.MaxValue)
                     .ThenBy(b => b.ReceivedAt))
        {
            if (remaining <= 0) break;
            var take = Math.Min(batch.RemainingQuantity, remaining);
            batch.Deplete(take);
            taken.Add((batch, take));
            remaining -= take;
        }

        QuantityOnHand -= quantity;
        return taken;
    }

    public void ApplyCorrection(decimal delta)
    {
        QuantityOnHand += delta;
        if (QuantityOnHand < 0) QuantityOnHand = 0;
    }

    public bool IsBelowReorderThreshold(decimal reorderThreshold) => QuantityOnHand < reorderThreshold;
}

public class StockBatch : BaseEntity
{
    public Guid StockLevelId { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal RemainingQuantity { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public decimal UnitCost { get; private set; }

    private StockBatch() { }

    internal StockBatch(Guid stockLevelId, decimal quantity, DateTimeOffset receivedAt, DateTimeOffset? expiresAt, decimal unitCost)
    {
        StockLevelId = stockLevelId;
        Quantity = quantity;
        RemainingQuantity = quantity;
        ReceivedAt = receivedAt;
        ExpiresAt = expiresAt;
        UnitCost = unitCost;
    }

    internal void Deplete(decimal quantity) => RemainingQuantity -= quantity;

    public bool IsExpired(DateTimeOffset at) => ExpiresAt is not null && at > ExpiresAt;
}

public class StockMovement : TenantAuditableEntity
{
    public Guid WarehouseId { get; private set; }
    public Guid IngredientId { get; private set; }
    public StockMovementType Type { get; private set; }
    public decimal Quantity { get; private set; } // positive = in, negative = out
    public string? Reference { get; private set; } // order id, purchase order id, etc.
    public Guid? PerformedByEmployeeId { get; private set; }

    private StockMovement() { }

    public StockMovement(Guid tenantId, Guid warehouseId, Guid ingredientId, StockMovementType type, decimal quantity, string? reference, Guid? performedByEmployeeId)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        IngredientId = ingredientId;
        Type = type;
        Quantity = quantity;
        Reference = reference;
        PerformedByEmployeeId = performedByEmployeeId;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}

public class StockTransfer : TenantAuditableEntity
{
    public Guid SourceWarehouseId { get; private set; }
    public Guid DestinationWarehouseId { get; private set; }
    public Guid IngredientId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTimeOffset TransferredAt { get; private set; }
    public Guid RequestedByEmployeeId { get; private set; }

    private StockTransfer() { }

    public StockTransfer(Guid tenantId, Guid sourceWarehouseId, Guid destinationWarehouseId, Guid ingredientId, decimal quantity, Guid requestedByEmployeeId)
    {
        if (sourceWarehouseId == destinationWarehouseId) throw new DomainException("Source and destination warehouses must differ.");
        TenantId = tenantId;
        SourceWarehouseId = sourceWarehouseId;
        DestinationWarehouseId = destinationWarehouseId;
        IngredientId = ingredientId;
        Quantity = quantity;
        RequestedByEmployeeId = requestedByEmployeeId;
        TransferredAt = DateTimeOffset.UtcNow;
    }
}

public class WasteRecord : TenantAuditableEntity
{
    public Guid WarehouseId { get; private set; }
    public Guid IngredientId { get; private set; }
    public decimal Quantity { get; private set; }
    public string Reason { get; private set; } = default!; // Expired, Spoiled, Prep error, Dropped...
    public Guid RecordedByEmployeeId { get; private set; }

    private WasteRecord() { }

    public WasteRecord(Guid tenantId, Guid warehouseId, Guid ingredientId, decimal quantity, string reason, Guid recordedByEmployeeId)
    {
        TenantId = tenantId;
        WarehouseId = warehouseId;
        IngredientId = ingredientId;
        Quantity = quantity;
        Reason = reason;
        RecordedByEmployeeId = recordedByEmployeeId;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}
