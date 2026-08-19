using EventForge.Api.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace EventForge.Api.Data;

public sealed class MongoDatabaseProvider
{
    public MongoDatabaseProvider(IOptions<MongoOptions> options)
    {
        var settings = options.Value;
        Client = new MongoClient(settings.ConnectionString);
        Database = Client.GetDatabase(settings.DatabaseName);
    }

    public MongoClient Client { get; }

    public IMongoDatabase Database { get; }
}
