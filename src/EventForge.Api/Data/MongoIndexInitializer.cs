using EventForge.Api.Models;
using MongoDB.Driver;

namespace EventForge.Api.Data;

public sealed class MongoIndexInitializer(
    MongoDatabaseProvider databaseProvider,
    ILogger<MongoIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var users = databaseProvider.Database.GetCollection<UserDocument>("users");
        var keys = Builders<UserDocument>.IndexKeys.Ascending(user => user.Email);
        await users.Indexes.CreateOneAsync(
            new CreateIndexModel<UserDocument>(keys, new CreateIndexOptions
            {
                Name = "ux_users_email",
                Unique = true
            }),
            cancellationToken: cancellationToken);

        logger.LogInformation("MongoDB indexes initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
