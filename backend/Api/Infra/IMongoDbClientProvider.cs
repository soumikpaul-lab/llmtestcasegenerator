using System;
using MongoDB.Driver;

namespace Api.Infra;

public interface IMongoDbClientProvider
{
    IMongoDatabase GetMongoDatabase(string key);

}
