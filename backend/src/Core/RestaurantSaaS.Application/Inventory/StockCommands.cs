using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Exceptions;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Inventory;

namespace RestaurantSaaS.Application.Inventory;

public sealed record CreateWarehouseCommand(Guid TenantId, Guid LocationId, string Name) : IRequest<Guid>, ITenantScopedRequest;

public sealed class CreateWarehouseCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateWarehouseCommand, Guid>
{
    public async Task<Guid> Handle(CreateWarehouseCommand request, CancellationToken ct)
    {
        var warehouse = new Warehouse(request.TenantId, request.LocationId, request.Name);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(ct);
        return warehouse.Id;
    }
}

public sealed record CreateIngredientCommand(Guid TenantId, string Name, MeasurementUnit Unit, decimal ReorderThreshold, decimal CostPerUnit, string? Barcode)
    : IRequest<Guid>, ITenantScopedRequest;

public sealed class CreateIngredientCommandValidator : AbstractValidator<CreateIngredientCommand>
{
    public CreateIngredientCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ReorderThreshold).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CostPerUnit).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateIngredientCommandHandler(IApplicationDbContext db) : IRequestHandler<CreateIngredientCommand, Guid>
{
    public async Task<Guid> Handle(CreateIngredientCommand request, CancellationToken ct)
    {
        var ingredient = new Ingredient(request.TenantId, request.Name, request.Unit, request.ReorderThreshold, request.CostPerUnit, request.Barcode);
        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync(ct);
        return ingredient.Id;
    }
}

public sealed record ReceiveStockCommand(Guid TenantId, Guid WarehouseId, Guid IngredientId, decimal Quantity, decimal UnitCost, DateTimeOffset? ExpiresAt, Guid PerformedByEmployeeId)
    : IRequest, ITenantScopedRequest;

public sealed class ReceiveStockCommandValidator : AbstractValidator<ReceiveStockCommand>
{
    public ReceiveStockCommandValidator() => RuleFor(x => x.Quantity).GreaterThan(0);
}

public sealed class ReceiveStockCommandHandler(IApplicationDbContext db) : IRequestHandler<ReceiveStockCommand>
{
    public async Task Handle(ReceiveStockCommand request, CancellationToken ct)
    {
        var stock = await db.Set<StockLevel>().Include(s => s.Batches)
            .SingleOrDefaultAsync(s => s.WarehouseId == request.WarehouseId && s.IngredientId == request.IngredientId, ct);

        if (stock is null)
        {
            stock = new StockLevel(request.TenantId, request.WarehouseId, request.IngredientId);
            db.Set<StockLevel>().Add(stock);
        }

        stock.Receive(request.Quantity, request.ExpiresAt, request.UnitCost);

        db.StockMovements.Add(new StockMovement(request.TenantId, request.WarehouseId, request.IngredientId,
            StockMovementType.Receipt, request.Quantity, reference: null, performedByEmployeeId: request.PerformedByEmployeeId));

        await db.SaveChangesAsync(ct);
    }
}

public sealed record AdjustStockCommand(Guid TenantId, Guid WarehouseId, Guid IngredientId, decimal Delta, string Reason, Guid PerformedByEmployeeId)
    : IRequest, ITenantScopedRequest;

public sealed class AdjustStockCommandHandler(IApplicationDbContext db) : IRequestHandler<AdjustStockCommand>
{
    public async Task Handle(AdjustStockCommand request, CancellationToken ct)
    {
        var stock = await db.Set<StockLevel>().SingleOrDefaultAsync(s => s.WarehouseId == request.WarehouseId && s.IngredientId == request.IngredientId, ct)
            ?? throw new NotFoundException(nameof(StockLevel), $"{request.WarehouseId}/{request.IngredientId}");

        stock.ApplyCorrection(request.Delta);

        db.StockMovements.Add(new StockMovement(request.TenantId, request.WarehouseId, request.IngredientId,
            StockMovementType.Correction, request.Delta, reference: request.Reason, performedByEmployeeId: request.PerformedByEmployeeId));

        await db.SaveChangesAsync(ct);
    }
}

public sealed record TransferStockCommand(Guid TenantId, Guid SourceWarehouseId, Guid DestinationWarehouseId, Guid IngredientId, decimal Quantity, Guid RequestedByEmployeeId)
    : IRequest, ITenantScopedRequest;

public sealed class TransferStockCommandHandler(IApplicationDbContext db) : IRequestHandler<TransferStockCommand>
{
    public async Task Handle(TransferStockCommand request, CancellationToken ct)
    {
        var source = await db.Set<StockLevel>().Include(s => s.Batches)
            .SingleOrDefaultAsync(s => s.WarehouseId == request.SourceWarehouseId && s.IngredientId == request.IngredientId, ct)
            ?? throw new NotFoundException(nameof(StockLevel), $"{request.SourceWarehouseId}/{request.IngredientId}");

        var ingredientName = (await db.Ingredients.SingleAsync(i => i.Id == request.IngredientId, ct)).Name;
        source.Consume(request.Quantity, ingredientName);

        var destination = await db.Set<StockLevel>().SingleOrDefaultAsync(s => s.WarehouseId == request.DestinationWarehouseId && s.IngredientId == request.IngredientId, ct);
        if (destination is null)
        {
            destination = new StockLevel(request.TenantId, request.DestinationWarehouseId, request.IngredientId);
            db.Set<StockLevel>().Add(destination);
        }
        destination.Receive(request.Quantity, expiresAt: null, unitCost: 0m);

        db.Set<StockTransfer>().Add(new StockTransfer(request.TenantId, request.SourceWarehouseId, request.DestinationWarehouseId,
            request.IngredientId, request.Quantity, request.RequestedByEmployeeId));

        await db.SaveChangesAsync(ct);
    }
}

public sealed record RecordWasteCommand(Guid TenantId, Guid WarehouseId, Guid IngredientId, decimal Quantity, string Reason, Guid RecordedByEmployeeId)
    : IRequest, ITenantScopedRequest;

public sealed class RecordWasteCommandHandler(IApplicationDbContext db) : IRequestHandler<RecordWasteCommand>
{
    public async Task Handle(RecordWasteCommand request, CancellationToken ct)
    {
        var stock = await db.Set<StockLevel>().Include(s => s.Batches)
            .SingleOrDefaultAsync(s => s.WarehouseId == request.WarehouseId && s.IngredientId == request.IngredientId, ct)
            ?? throw new NotFoundException(nameof(StockLevel), $"{request.WarehouseId}/{request.IngredientId}");

        var ingredientName = (await db.Ingredients.SingleAsync(i => i.Id == request.IngredientId, ct)).Name;
        stock.Consume(request.Quantity, ingredientName);

        db.Set<WasteRecord>().Add(new WasteRecord(request.TenantId, request.WarehouseId, request.IngredientId, request.Quantity, request.Reason, request.RecordedByEmployeeId));

        db.StockMovements.Add(new StockMovement(request.TenantId, request.WarehouseId, request.IngredientId,
            StockMovementType.Waste, -request.Quantity, reference: request.Reason, performedByEmployeeId: request.RecordedByEmployeeId));

        await db.SaveChangesAsync(ct);
    }
}
