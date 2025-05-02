using System;

namespace Api.Infra;

public class MongoDbSettings
{
    public Dictionary<string, SingleMongoDbSettings> Databases { get; set; } = [];
}

public class SingleMongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
}
