using DotNetEnv;
using EventForge.Api.Configuration;
using EventForge.Api.Data;
using EventForge.Api.Health;
using EventForge.Api.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddInMemoryCollection(EnvironmentConfiguration.BuildOverrides());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions<MongoOptions>()
    .BindConfiguration(MongoOptions.SectionName)
    .Validate(options => Uri.TryCreate(options.ConnectionString, UriKind.Absolute, out _),
        "Mongo:ConnectionString must be a valid MongoDB connection string.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName),
        "Mongo:DatabaseName is required.")
    .ValidateOnStart();

builder.Services.AddOptions<RedisOptions>()
    .BindConfiguration(RedisOptions.SectionName)
    .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
        "Redis:ConnectionString is required.")
    .ValidateOnStart();

builder.Services.AddSingleton<MongoDatabaseProvider>();
builder.Services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<RedisOptions>>().Value;
    var configuration = ConfigurationOptions.Parse(settings.ConnectionString, true);
    configuration.AbortOnConnectFail = false;
    configuration.ConnectRetry = 2;
    return ConnectionMultiplexer.Connect(configuration);
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddHealthChecks()
    .AddCheck<MongoHealthCheck>("mongodb")
    .AddCheck<RedisHealthCheck>("redis");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();

app.MapGet("/", () => Results.Ok(new
{
    service = "EventForge API",
    version = "0.2.0",
    status = "healthy",
    documentation = "/swagger"
}));

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready");

app.MapControllers();

app.Run();

public partial class Program;
