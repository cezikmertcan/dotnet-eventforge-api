using MongoDB.Driver;

namespace EventForge.Api.Data;

public interface IMongoRepository<T> where T : MongoDocument
{
    IMongoCollection<T> Collection { get; }

    Task<T?> FindByIdAsync(string id, CancellationToken cancellationToken = default);

    Task<T?> FindOneAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(
        FilterDefinition<T>? filter = null,
        SortDefinition<T>? sort = null,
        CancellationToken cancellationToken = default);

    Task<T> InsertAsync(T document, CancellationToken cancellationToken = default);

    Task<bool> ReplaceAsync(T document, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class MongoRepository<T>(MongoDatabaseProvider databaseProvider) : IMongoRepository<T>
    where T : MongoDocument
{
    private readonly FilterDefinitionBuilder<T> _filters = Builders<T>.Filter;

    public IMongoCollection<T> Collection { get; } = databaseProvider.Database.GetCollection<T>(GetCollectionName());

    public Task<T?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
        => FindOneAsync(_filters.Eq(document => document.Id, id), cancellationToken);

    public async Task<T?> FindOneAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
    {
        var result = await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<T>> ListAsync(
        FilterDefinition<T>? filter = null,
        SortDefinition<T>? sort = null,
        CancellationToken cancellationToken = default)
    {
        var query = Collection.Find(filter ?? _filters.Empty);
        if (sort is not null)
        {
            query = query.Sort(sort);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<T> InsertAsync(T document, CancellationToken cancellationToken = default)
    {
        document.CreatedAtUtc = DateTime.UtcNow;
        document.UpdatedAtUtc = document.CreatedAtUtc;
        await Collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document;
    }

    public async Task<bool> ReplaceAsync(T document, CancellationToken cancellationToken = default)
    {
        document.UpdatedAtUtc = DateTime.UtcNow;
        var result = await Collection.ReplaceOneAsync(
            _filters.Eq(item => item.Id, document.Id),
            document,
            cancellationToken: cancellationToken);

        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await Collection.DeleteOneAsync(_filters.Eq(item => item.Id, id), cancellationToken);
        return result.DeletedCount > 0;
    }

    private static string GetCollectionName()
    {
        var attribute = typeof(T).GetCustomAttributes(typeof(MongoCollectionAttribute), false)
            .OfType<MongoCollectionAttribute>()
            .SingleOrDefault();

        return attribute?.Name ?? $"{typeof(T).Name.ToLowerInvariant()}s";
    }
}
