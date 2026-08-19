using System.Text.Json;
using EventForge.Api.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace EventForge.Api.Infrastructure;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan lifetime, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public sealed class RedisCacheService(
    IConnectionMultiplexer connection,
    IOptions<RedisOptions> options,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase _database = connection.GetDatabase();
    private readonly string _prefix = options.Value.InstanceName;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.StringGetAsync(BuildKey(key)).WaitAsync(cancellationToken);
            return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis read failed for cache key {CacheKey}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan lifetime, CancellationToken cancellationToken = default)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, JsonOptions);
            await _database.StringSetAsync(BuildKey(key), serialized, lifetime).WaitAsync(cancellationToken);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis write failed for cache key {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _database.KeyDeleteAsync(BuildKey(key)).WaitAsync(cancellationToken);
        }
        catch (RedisException exception)
        {
            logger.LogWarning(exception, "Redis invalidation failed for cache key {CacheKey}", key);
        }
    }

    private RedisKey BuildKey(string key) => $"{_prefix}{key}";
}
