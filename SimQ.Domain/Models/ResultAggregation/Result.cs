using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SimQ.Domain.Models.Base;

namespace SimQ.Domain.Models.ResultAggregation;

public class Result : IMongoObjectEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    
    [BsonElement("problemId")]
    public string ProblemId { get; set; }
    
    [BsonElement("problemName")]
    public string ProblemName { get; set; } = string.Empty;
    
    [BsonElement("taskId")]
    public string TaskId { get; set; }
    
    [BsonElement("text")]
    public string? Text { get; set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
