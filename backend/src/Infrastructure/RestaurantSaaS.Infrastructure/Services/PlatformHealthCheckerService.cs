using RestaurantSaaS.Application.Common.Interfaces;
using RestaurantSaaS.Domain.Enums;
using RestaurantSaaS.Infrastructure.Persistence;
using StackExchange.Redis;

namespace RestaurantSaaS.Infrastructure.Services;

public sealed class PlatformHealthCheckerService(ApplicationDbContext db, IConnectionMultiplexer redis) : IPlatformHealthChecker
{
    public async Task<IReadOnlyDictionary<PlatformComponent, ComponentHealth>> CheckAsync(CancellationToken ct)
    {
        var result = new Dictionary<PlatformComponent, ComponentHealth>
        {
            // If this code is executing, the API process itself is up; Realtime/BackgroundJobs aren't
            // independently probed here (that needs a synthetic SignalR client / Hangfire heartbeat job —
            // Phase 2), so they default Operational unless an open incident says otherwise.
            [PlatformComponent.Api] = ComponentHealth.Operational,
            [PlatformComponent.Realtime] = ComponentHealth.Operational,
            [PlatformComponent.BackgroundJobs] = ComponentHealth.Operational,
        };

        try
        {
            result[PlatformComponent.Database] = await db.Database.CanConnectAsync(ct)
                ? ComponentHealth.Operational
                : ComponentHealth.MajorOutage;
        }
        catch
        {
            result[PlatformComponent.Database] = ComponentHealth.MajorOutage;
        }

        try
        {
            var latency = await redis.GetDatabase().PingAsync();
            result[PlatformComponent.Cache] = latency.TotalMilliseconds < 500
                ? ComponentHealth.Operational
                : ComponentHealth.DegradedPerformance;
        }
        catch
        {
            result[PlatformComponent.Cache] = ComponentHealth.MajorOutage;
        }

        return result;
    }
}
