using System;
using MongoDB.Driver;

namespace Worker.Infra;

public interface IMongoDbClientProvider
{
    IMongoDatabase GetMongoDatabase(string key);
}
