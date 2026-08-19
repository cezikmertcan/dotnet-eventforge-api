using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace EventForge.Api.Health;

public sealed class RedisHealthCheck(IConnectionMultiplexer connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await connection.GetDatabase().PingAsync().WaitAsync(cancellationToken);
            return HealthCheckResult.Healthy("Redis is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis is unavailable.", exception);
        }
    }
}
