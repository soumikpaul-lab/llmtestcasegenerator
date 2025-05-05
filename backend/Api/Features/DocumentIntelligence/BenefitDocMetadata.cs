using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Api.Features.DocumentIntelligence;

public class BenefitDocMetadata
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string DocumentName { get; set; } = string.Empty;
    public DocumentProcessStatus DocumentProcessStatus { get; set; } = DocumentProcessStatus.NotStarted;
    public DateTime UploadDateTime { get; set; } = DateTime.UtcNow;
    public string User { get; set; } = string.Empty;
}

public class BenefitsStore
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DocumentName { get; set; } = string.Empty;
    public List<ExtractedBenefitResponse> ExtractedBenefitResponses { get; set; } = [];
}

public class ExtractedBenefitResponse
{

    [JsonPropertyName("benefit")]
    public string Benefit { get; set; } = string.Empty;
    [JsonPropertyName("innetwork")]
    public string InNetworkConditions { get; set; } = string.Empty;
    [JsonPropertyName("outofnetwork")]
    public string OutOfNetworkConditions { get; set; } = string.Empty;
    [JsonPropertyName("limitations")]
    public string Limitations { get; set; } = string.Empty;

    public override string ToString() => JsonSerializer.Serialize(this, new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    });

}


public class GeneratedTestCase
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // THIS maps the BSON field "type" → this CLR property
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    public string ExpectedResult { get; set; } = string.Empty;

    public string[] IcdCodes { get; set; } = Array.Empty<string>();

    public string[] ProcedureCodes { get; set; } = Array.Empty<string>();

    public string[] PlaceOfService { get; set; } = Array.Empty<string>();
}

public class TestCaseStore
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // [BsonElement("documentName")]
    public string DocumentName { get; set; } = string.Empty;

    // [BsonElement("generatedTestCases")]
    public Dictionary<string, List<GeneratedTestCase>> GeneratedTestCases
    { get; set; } = new Dictionary<string, List<GeneratedTestCase>>();
}

public enum DocumentProcessStatus
{
    NotStarted = 0,
    InQueue = 1,
    Processing = 2,
    Success = 3,
    Failed = 4

}

