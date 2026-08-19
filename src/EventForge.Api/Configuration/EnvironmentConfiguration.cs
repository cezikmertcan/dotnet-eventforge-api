namespace EventForge.Api.Configuration;

public static class EnvironmentConfiguration
{
    public static Dictionary<string, string?> BuildOverrides()
    {
        var overrides = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        Add(overrides, "Mongo:ConnectionString", "MONGO_CONNECTION_STRING");
        Add(overrides, "Mongo:DatabaseName", "MONGO_DATABASE_NAME");
        Add(overrides, "Redis:ConnectionString", "REDIS_CONNECTION_STRING");
        Add(overrides, "Redis:InstanceName", "REDIS_INSTANCE_NAME");

        Add(overrides, "Jwt:Issuer", "JWT_ISSUER");
        Add(overrides, "Jwt:Audience", "JWT_AUDIENCE");
        Add(overrides, "Jwt:SigningKey", "JWT_SIGNING_KEY");
        Add(overrides, "Jwt:AccessTokenMinutes", "JWT_ACCESS_TOKEN_MINUTES");

        Add(overrides, "SeedAdmin:Email", "SEED_ADMIN_EMAIL");
        Add(overrides, "SeedAdmin:Password", "SEED_ADMIN_PASSWORD");
        Add(overrides, "SeedAdmin:DisplayName", "SEED_ADMIN_DISPLAY_NAME");

        return overrides;
    }

    private static void Add(IDictionary<string, string?> target, string configurationKey, string environmentKey)
    {
        var value = Environment.GetEnvironmentVariable(environmentKey);
        if (!string.IsNullOrWhiteSpace(value))
        {
            target[configurationKey] = value;
        }
    }
}
