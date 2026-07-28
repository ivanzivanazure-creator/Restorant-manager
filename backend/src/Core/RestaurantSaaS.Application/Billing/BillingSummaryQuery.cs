using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSaaS.Application.Common.Behaviors;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Subscription;

namespace RestaurantSaaS.Application.Billing;

public sealed record BillingSummaryDto(
    bool StripeConnected, bool OnboardingComplete, decimal TransactionFeePercent,
    decimal FeesChargedThisMonth, int TransactionCountThisMonth, decimal FeesChargedAllTime);

public sealed record GetBillingSummaryQuery(Guid TenantId) : IRequest<BillingSummaryDto>, ITenantScopedRequest;

public sealed class GetBillingSummaryQueryHandler(IApplicationDbContext db, IDateTimeProvider dateTime)
    : IRequestHandler<GetBillingSummaryQuery, BillingSummaryDto>
{
    public async Task<BillingSummaryDto> Handle(GetBillingSummaryQuery request, CancellationToken ct)
    {
        var tenant = await db.RestaurantOwners.SingleAsync(t => t.Id == request.TenantId, ct);
        var subscription = await db.Set<TenantSubscription>().SingleAsync(s => s.TenantId == request.TenantId, ct);
        var package = await db.Packages.SingleAsync(p => p.Id == subscription.PackageId, ct);

        var monthStart = new DateTimeOffset(dateTime.UtcNow.Year, dateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var ledgerEntries = await db.PlatformFeeLedgerEntries
            .Where(e => e.TenantId == request.TenantId)
            .ToListAsync(ct);

        var thisMonth = ledgerEntries.Where(e => e.CreatedAt >= monthStart).ToList();

        return new BillingSummaryDto(
            tenant.StripeConnectedAccountId is not null,
            tenant.StripeOnboardingComplete,
            package.TransactionFeePercent,
            thisMonth.Sum(e => e.FeeAmount),
            thisMonth.Count,
            ledgerEntries.Sum(e => e.FeeAmount));
    }
}
