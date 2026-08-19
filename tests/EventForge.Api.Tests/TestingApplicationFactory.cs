using EventForge.Api.Authentication;
using EventForge.Api.Data;
using EventForge.Api.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventForge.Api.Tests;

public sealed class TestingApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = "mongodb://127.0.0.1:27017",
                ["Mongo:DatabaseName"] = "eventforge-tests",
                ["Redis:ConnectionString"] = "127.0.0.1:6379",
                ["Redis:InstanceName"] = "eventforge-tests:",
                ["Jwt:Issuer"] = "EventForge.Tests",
                ["Jwt:Audience"] = "EventForge.TestClient",
                ["Jwt:SigningKey"] = "test-signing-key-that-is-at-least-32-characters-long",
                ["Jwt:AccessTokenMinutes"] = "15"
            });
        });
        builder.ConfigureServices(services =>
        {
            foreach (var descriptor in services
                         .Where(descriptor => descriptor.ImplementationType == typeof(MongoIndexInitializer)
                            || descriptor.ImplementationType == typeof(SeedAdminService))
                         .ToList())
            {
                services.Remove(descriptor);
            }
        });
    }
}
