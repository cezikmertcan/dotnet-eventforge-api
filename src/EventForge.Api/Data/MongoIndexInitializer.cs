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

        var events = databaseProvider.Database.GetCollection<EventDocument>("events");
        await events.Indexes.CreateOneAsync(
            new CreateIndexModel<EventDocument>(
                Builders<EventDocument>.IndexKeys.Ascending(item => item.Slug),
                new CreateIndexOptions { Name = "ux_events_slug", Unique = true }),
            cancellationToken: cancellationToken);
        await events.Indexes.CreateOneAsync(
            new CreateIndexModel<EventDocument>(
                Builders<EventDocument>.IndexKeys.Ascending(item => item.StartsAtUtc),
                new CreateIndexOptions { Name = "ix_events_starts_at" }),
            cancellationToken: cancellationToken);

        var sessions = databaseProvider.Database.GetCollection<SessionDocument>("sessions");
        await sessions.Indexes.CreateOneAsync(
            new CreateIndexModel<SessionDocument>(
                Builders<SessionDocument>.IndexKeys.Ascending(item => item.EventId).Ascending(item => item.StartsAtUtc),
                new CreateIndexOptions { Name = "ix_sessions_event_schedule" }),
            cancellationToken: cancellationToken);

        var registrations = databaseProvider.Database.GetCollection<RegistrationDocument>("registrations");
        await registrations.Indexes.CreateOneAsync(
            new CreateIndexModel<RegistrationDocument>(
                Builders<RegistrationDocument>.IndexKeys.Ascending(item => item.EventId).Ascending(item => item.AttendeeId),
                new CreateIndexOptions { Name = "ux_registrations_event_attendee", Unique = true }),
            cancellationToken: cancellationToken);

        logger.LogInformation("MongoDB indexes initialized for users, events, sessions, and registrations.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
