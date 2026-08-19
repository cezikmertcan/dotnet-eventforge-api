using System.Net;

namespace EventForge.Api.Tests;

public sealed class ApiSmokeTests(TestingApplicationFactory factory) : IClassFixture<TestingApplicationFactory>
{
    [Fact]
    public async Task RootEndpointDescribesTheService()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("EventForge API", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LivenessEndpointDoesNotRequireDatabases()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
