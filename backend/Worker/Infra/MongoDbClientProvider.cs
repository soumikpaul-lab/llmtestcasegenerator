using System;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Worker.Infra;

public class MongoDbClientProvider : IMongoDbClientProvider
{
    private readonly IDictionary<string, IMongoDatabase> _databases;

    public MongoDbClientProvider(IOptions<MongoDbSettings> options)
    {
        var settings = options.Value;

        _databases = settings.Databases.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var mongoClient = new MongoClient(kvp.Value.ConnectionString);
                return mongoClient.GetDatabase(kvp.Value.DatabaseName);
            }
        );

    }
    public IMongoDatabase GetMongoDatabase(string key)
    {
        if (!_databases.TryGetValue(key, out var database))
        {
            throw new ArgumentException($"Database configuration with key '{key}' not found.");
        }

        return database;
    }
}
