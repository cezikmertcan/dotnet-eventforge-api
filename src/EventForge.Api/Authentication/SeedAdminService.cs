using EventForge.Api.Configuration;
using EventForge.Api.Data;
using EventForge.Api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace EventForge.Api.Authentication;

public sealed class SeedAdminService(
    IMongoRepository<UserDocument> users,
    IOptions<SeedAdminOptions> options,
    ILogger<SeedAdminService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Email) && string.IsNullOrWhiteSpace(settings.Password))
        {
            logger.LogInformation("Admin bootstrap is disabled because seed credentials are not configured.");
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
        {
            throw new InvalidOperationException("SEED_ADMIN_EMAIL and SEED_ADMIN_PASSWORD must be configured together.");
        }

        var email = settings.Email.Trim().ToLowerInvariant();
        var filter = Builders<UserDocument>.Filter.Eq(user => user.Email, email);
        var existing = await users.FindOneAsync(filter, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.Role, RoleNames.Admin, StringComparison.Ordinal))
            {
                existing.Role = RoleNames.Admin;
                await users.ReplaceAsync(existing, cancellationToken);
            }

            logger.LogInformation("Bootstrap admin {Email} is ready.", email);
            return;
        }

        await users.InsertAsync(new UserDocument
        {
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(settings.DisplayName) ? "EventForge Admin" : settings.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(settings.Password),
            Role = RoleNames.Admin
        }, cancellationToken);

        logger.LogInformation("Bootstrap admin {Email} was created.", email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
