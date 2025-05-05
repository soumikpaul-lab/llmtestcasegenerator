using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Worker.Features.DocumentIntelligence;

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

public enum DocumentProcessStatus
{
    NotStarted = 0,
    InQueue = 1,
    Processing = 2,
    Success = 3,
    Failed = 4

}