using System.Text;
using DotNetEnv;
using EventForge.Api.Authentication;
using EventForge.Api.Configuration;
using EventForge.Api.Data;
using EventForge.Api.Health;
using EventForge.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddInMemoryCollection(EnvironmentConfiguration.BuildOverrides());

var jwtSettings = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var signingKey = string.IsNullOrWhiteSpace(jwtSettings.SigningKey)
    ? "invalid-signing-key-placeholder-for-options-validation-only"
    : jwtSettings.SigningKey;

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

builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "Jwt:Issuer is required.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Jwt:Audience is required.")
    .Validate(options => options.SigningKey.Length >= 32, "Jwt:SigningKey must contain at least 32 characters.")
    .Validate(options => options.AccessTokenMinutes is > 0 and <= 1440, "Jwt:AccessTokenMinutes must be between 1 and 1440.")
    .ValidateOnStart();

builder.Services.AddOptions<SeedAdminOptions>().BindConfiguration(SeedAdminOptions.SectionName);

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
builder.Services.AddSingleton(typeof(IMongoRepository<>), typeof(MongoRepository<>));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddHostedService<MongoIndexInitializer>();
builder.Services.AddHostedService<SeedAdminService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();
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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
