using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SimQ.Domain.Models.Base;

namespace SimQ.Domain.Models.TaskAggregation;

public class SimulationTask : IMongoObjectEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    
    [BsonElement("problemId")]
    public string ProblemId { get; set; }
    
    [BsonElement("problemName")]
    public string ProblemName { get; set; } = string.Empty;
    
    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public SimulationTaskStatus Status { get; set; }
    
    [BsonElement("startTime")]
    public DateTime? StartTime { get; set; }
    
    [BsonElement("endTime")]
    public DateTime? EndTime { get; set; }
    
    [BsonElement("results")]
    public string? Results { get; set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public enum SimulationTaskStatus {
    Waiting,
    Modelling,
    Error, 
    Canceled,
    Completed 
}
