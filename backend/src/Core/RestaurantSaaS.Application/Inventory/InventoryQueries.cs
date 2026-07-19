using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Interfaces;

namespace RestaurantSaaS.Application.Inventory;

public sealed record StockLevelDto(Guid IngredientId, string IngredientName, Guid WarehouseId, string WarehouseName, decimal QuantityOnHand, string Unit, decimal ReorderThreshold, bool IsBelowThreshold);

public sealed record GetStockLevelsQuery(Guid TenantId, Guid WarehouseId) : IRequest<IReadOnlyCollection<StockLevelDto>>, ITenantScopedRequest;

public sealed class GetStockLevelsQueryHandler(IApplicationDbContext db) : IRequestHandler<GetStockLevelsQuery, IReadOnlyCollection<StockLevelDto>>
{
    public async Task<IReadOnlyCollection<StockLevelDto>> Handle(GetStockLevelsQuery request, CancellationToken ct)
    {
        var query =
            from stock in db.Set<Domain.Inventory.StockLevel>()
            join ingredient in db.Ingredients on stock.IngredientId equals ingredient.Id
            join warehouse in db.Warehouses on stock.WarehouseId equals warehouse.Id
            where stock.TenantId == request.TenantId && stock.WarehouseId == request.WarehouseId
            select new StockLevelDto(ingredient.Id, ingredient.Name, warehouse.Id, warehouse.Name,
                stock.QuantityOnHand, ingredient.Unit.ToString(), ingredient.ReorderThreshold,
                stock.QuantityOnHand < ingredient.ReorderThreshold);

        return await query.ToListAsync(ct);
    }
}

public sealed record GetLowStockAlertsQuery(Guid TenantId) : IRequest<IReadOnlyCollection<StockLevelDto>>, ITenantScopedRequest;

public sealed class GetLowStockAlertsQueryHandler(IApplicationDbContext db) : IRequestHandler<GetLowStockAlertsQuery, IReadOnlyCollection<StockLevelDto>>
{
    public async Task<IReadOnlyCollection<StockLevelDto>> Handle(GetLowStockAlertsQuery request, CancellationToken ct)
    {
        var query =
            from stock in db.Set<Domain.Inventory.StockLevel>()
            join ingredient in db.Ingredients on stock.IngredientId equals ingredient.Id
            join warehouse in db.Warehouses on stock.WarehouseId equals warehouse.Id
            where stock.TenantId == request.TenantId && stock.QuantityOnHand < ingredient.ReorderThreshold
            select new StockLevelDto(ingredient.Id, ingredient.Name, warehouse.Id, warehouse.Name,
                stock.QuantityOnHand, ingredient.Unit.ToString(), ingredient.ReorderThreshold, true);

        return await query.ToListAsync(ct);
    }
}
