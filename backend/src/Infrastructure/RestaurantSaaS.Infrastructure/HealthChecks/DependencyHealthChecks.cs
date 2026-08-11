using Microsoft.Extensions.Diagnostics.HealthChecks;
using RestaurantSaaS.Infrastructure.Persistence;
using StackExchange.Redis;

namespace RestaurantSaaS.Infrastructure.HealthChecks;

/// <summary>Resolves ApplicationDbContext from DI at check time (not a captured connection string), so it
/// always uses whatever connection the app is actually running with — including a test host's
/// WebApplicationFactory override.</summary>
public sealed class PostgresHealthCheck(ApplicationDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Cannot connect to Postgres.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Cannot connect to Postgres.", exception);
        }
    }
}

/// <summary>Resolves IConnectionMultiplexer from DI at check time, same reasoning as PostgresHealthCheck.</summary>
public sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Cannot connect to Redis.", exception);
        }
    }
}
