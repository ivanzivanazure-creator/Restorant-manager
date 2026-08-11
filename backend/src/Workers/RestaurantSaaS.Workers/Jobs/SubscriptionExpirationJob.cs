using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Subscription;

namespace RestaurantSaaS.Workers.Jobs;

/// <summary>Hourly: locks any tenant whose trial or billing period has lapsed past the grace period.
/// A locked tenant's staff can still log in (to see the "subscription expired" screen / billing page)
/// but every tenant-scoped write is rejected — see TenantSubscription.EnsureActiveOrThrow, called from
/// the TenantAuthorizationBehavior... actually invoked explicitly by write-side handlers that need it.</summary>
public sealed class SubscriptionExpirationJob(IApplicationDbContext db, IDateTimeProvider dateTime, ILogger<SubscriptionExpirationJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = dateTime.UtcNow;
        var subscriptions = await db.Set<TenantSubscription>().ToListAsync(ct);

        var lockedCount = 0;
        foreach (var subscription in subscriptions)
        {
            if (subscription.TryAutoLock(now))
            {
                lockedCount++;
                var tenant = await db.RestaurantOwners.SingleAsync(t => t.Id == subscription.TenantId, ct);
                tenant.Suspend();
            }
        }

        if (lockedCount > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogWarning("SubscriptionExpirationJob locked {Count} tenant(s) for lapsed billing", lockedCount);
        }
    }
}
