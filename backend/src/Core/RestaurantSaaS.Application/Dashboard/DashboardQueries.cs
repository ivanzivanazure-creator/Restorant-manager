using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;

namespace RestaurantSaaS.Application.Dashboard;

public sealed record KitchenStatusSummaryDto(int Queued, int InProgress, int Ready, int Overdue);
public sealed record InventoryAlertDto(Guid IngredientId, string IngredientName, decimal QuantityOnHand, decimal ReorderThreshold);

public sealed record DashboardSummaryDto(
    decimal TodayRevenue, int TodayOrderCount, decimal TodayAverageOrderValue,
    decimal Last7DaysRevenue, KitchenStatusSummaryDto Kitchen, IReadOnlyCollection<InventoryAlertDto> InventoryAlerts);

public sealed record GetDashboardSummaryQuery(Guid TenantId, Guid LocationId) : IRequest<DashboardSummaryDto>, ITenantScopedRequest;

public sealed class GetDashboardSummaryQueryHandler(IApplicationDbContext db, IDateTimeProvider dateTime)
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime);
        var sevenDaysAgo = today.AddDays(-6);

        var paidOrdersToday = await db.Orders
            .Where(o => o.LocationId == request.LocationId && o.TenantId == request.TenantId
                && o.Status == OrderStatus.Paid && o.ClosedAt != null
                && o.ClosedAt.Value.UtcDateTime.Date == today.ToDateTime(TimeOnly.MinValue))
            .ToListAsync(ct);

        var todayRevenue = paidOrdersToday.Sum(o => o.GrandTotal);
        var todayOrderCount = paidOrdersToday.Count;
        var todayAov = todayOrderCount == 0 ? 0 : todayRevenue / todayOrderCount;

        var last7DaysRevenue = await db.Set<Domain.Reporting.DailySalesSummary>()
            .Where(s => s.TenantId == request.TenantId && s.LocationId == request.LocationId && s.Date >= sevenDaysAgo && s.Date <= today)
            .SumAsync(s => s.NetRevenue, ct);
        // Today's own revenue is computed live above (the nightly job hasn't materialized it yet);
        // add it on top of the historical (already-materialized) days.
        last7DaysRevenue += todayRevenue;

        var kitchenTickets = await db.KitchenTickets
            .Where(t => t.LocationId == request.LocationId && t.TenantId == request.TenantId
                && t.Status != KitchenTicketStatus.Served && t.Status != KitchenTicketStatus.Cancelled)
            .ToListAsync(ct);

        var kitchenSummary = new KitchenStatusSummaryDto(
            kitchenTickets.Count(t => t.Status == KitchenTicketStatus.Queued),
            kitchenTickets.Count(t => t.Status == KitchenTicketStatus.InProgress),
            kitchenTickets.Count(t => t.Status == KitchenTicketStatus.Ready),
            kitchenTickets.Count(t => t.IsOverdue));

        var alerts = await (
            from stock in db.Set<Domain.Inventory.StockLevel>()
            join ingredient in db.Ingredients on stock.IngredientId equals ingredient.Id
            join warehouse in db.Warehouses on stock.WarehouseId equals warehouse.Id
            where stock.TenantId == request.TenantId && warehouse.LocationId == request.LocationId
                  && stock.QuantityOnHand < ingredient.ReorderThreshold
            select new InventoryAlertDto(ingredient.Id, ingredient.Name, stock.QuantityOnHand, ingredient.ReorderThreshold)
        ).ToListAsync(ct);

        return new DashboardSummaryDto(todayRevenue, todayOrderCount, todayAov, last7DaysRevenue, kitchenSummary, alerts);
    }
}
