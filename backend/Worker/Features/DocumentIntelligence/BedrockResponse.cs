using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Worker.Features.DocumentIntelligence;

public class BenefitNameResponse
{
    [JsonPropertyName("benefits")]
    public BenefitName[] Benefits { get; set; } = [];
}

public class BenefitsStore
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DocumentName { get; set; } = string.Empty;
    public List<ExtractedBenefitResponse> ExtractedBenefitResponses { get; set; } = [];
}

public class BenefitName
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
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

    public override string ToString()
    {
        return JsonSerializer.Serialize<ExtractedBenefitResponse>(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });
    }

}

public class GeneratedTestCase
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public string ExpectedResult { get; set; } = string.Empty;
    public string[] IcdCodes { get; set; } = [];
    public string[] ProcedureCodes { get; set; } = [];
    public string[] PlaceOfService { get; set; } = [];
}

public class TestCaseStore
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DocumentName { get; set; } = string.Empty;
    public Dictionary<string, List<GeneratedTestCase>> GeneratedTestCases { get; set; } = [];
}

