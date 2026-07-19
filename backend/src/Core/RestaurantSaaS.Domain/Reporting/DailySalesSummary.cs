using RestaurantSaaS.Domain.Common;

namespace RestaurantSaaS.Domain.Reporting;

/// <summary>Materialized nightly by DailyReportAggregationJob so Dashboard/Reports queries never scan raw Orders.</summary>
public class DailySalesSummary : TenantAuditableEntity
{
    public Guid LocationId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal GrossRevenue { get; private set; }
    public decimal NetRevenue { get; private set; }
    public decimal TaxCollected { get; private set; }
    public decimal DiscountsGiven { get; private set; }
    public decimal RefundsIssued { get; private set; }
    public int OrderCount { get; private set; }
    public int CoverCount { get; private set; }
    public decimal AverageOrderValue => OrderCount == 0 ? 0 : NetRevenue / OrderCount;

    private DailySalesSummary() { }

    public DailySalesSummary(Guid tenantId, Guid locationId, DateOnly date, decimal grossRevenue, decimal netRevenue,
        decimal taxCollected, decimal discountsGiven, decimal refundsIssued, int orderCount, int coverCount)
    {
        TenantId = tenantId;
        LocationId = locationId;
        Date = date;
        GrossRevenue = grossRevenue;
        NetRevenue = netRevenue;
        TaxCollected = taxCollected;
        DiscountsGiven = discountsGiven;
        RefundsIssued = refundsIssued;
        OrderCount = orderCount;
        CoverCount = coverCount;
    }
}
