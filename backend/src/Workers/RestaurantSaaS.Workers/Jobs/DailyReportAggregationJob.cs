using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Domain.Reporting;

namespace RestaurantSaaS.Workers.Jobs;

/// <summary>Nightly: materializes a DailySalesSummary row per (tenant, location) for the day that just
/// ended, so Dashboard/Reports queries never scan raw Orders for historical days.</summary>
public sealed class DailyReportAggregationJob(IApplicationDbContext db, IDateTimeProvider dateTime, ILogger<DailyReportAggregationJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var yesterday = DateOnly.FromDateTime(dateTime.UtcNow.UtcDateTime.AddDays(-1));
        var dayStart = yesterday.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        var closedOrders = await db.Orders
            .Where(o => o.ClosedAt != null && o.ClosedAt >= dayStart && o.ClosedAt < dayEnd
                && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Cancelled))
            .ToListAsync(ct);

        var groups = closedOrders.GroupBy(o => new { o.TenantId, o.LocationId });

        var created = 0;
        foreach (var group in groups)
        {
            var alreadyExists = await db.Set<DailySalesSummary>()
                .AnyAsync(s => s.TenantId == group.Key.TenantId && s.LocationId == group.Key.LocationId && s.Date == yesterday, ct);
            if (alreadyExists) continue;

            var paidOrders = group.Where(o => o.Status == OrderStatus.Paid).ToList();
            var grossRevenue = paidOrders.Sum(o => o.Subtotal);
            var netRevenue = paidOrders.Sum(o => o.TaxableAmount);
            var taxCollected = paidOrders.Sum(o => o.TaxTotal);
            var discounts = paidOrders.Sum(o => o.DiscountTotal);

            var summary = new DailySalesSummary(group.Key.TenantId, group.Key.LocationId, yesterday,
                grossRevenue, netRevenue, taxCollected, discounts, refundsIssued: 0m,
                orderCount: paidOrders.Count, coverCount: paidOrders.Count);

            db.Set<DailySalesSummary>().Add(summary);
            created++;
        }

        if (created > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("DailyReportAggregationJob materialized {Count} daily summaries for {Date}", created, yesterday);
        }
    }
}
