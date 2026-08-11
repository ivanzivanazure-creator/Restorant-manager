using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using RestaurantSaaS.Application.Common.Interfaces;
using StackExchange.Redis;

namespace RestaurantSaaS.Infrastructure.Caching;

/// <summary>IDistributedCache (Redis-backed) wrapper for typed get/set, plus prefix invalidation via
/// the raw StackExchange.Redis connection (IDistributedCache alone can't scan keys).</summary>
public sealed class RedisCacheService(IDistributedCache cache, IConnectionMultiplexer redis) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(key, ct);
        return bytes is null ? default : JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(5),
        };
        await cache.SetAsync(key, bytes, options, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) => cache.RemoveAsync(key, ct);

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var endpoints = redis.GetEndPoints();
        if (endpoints.Length == 0) return;

        var server = redis.GetServer(endpoints[0]);
        var db = redis.GetDatabase();
        await foreach (var key in server.KeysAsync(pattern: $"{prefix}*"))
        {
            await db.KeyDeleteAsync(key);
        }
    }
}
