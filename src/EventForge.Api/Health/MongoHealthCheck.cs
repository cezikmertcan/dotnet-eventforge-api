using EventForge.Api.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EventForge.Api.Health;

public sealed class MongoHealthCheck(MongoDatabaseProvider mongo) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await mongo.Database.RunCommandAsync<BsonDocument>(
                new BsonDocumentCommand<BsonDocument>(new BsonDocument("ping", 1)),
                cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("MongoDB is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("MongoDB is unavailable.", exception);
        }
    }
}
