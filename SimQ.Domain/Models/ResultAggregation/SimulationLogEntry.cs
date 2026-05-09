using MongoDB.Bson.Serialization.Attributes;

namespace SimQ.Domain.Models.ResultAggregation;

public class SimulationLogEntry
{
    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("level")]
    public string Level { get; set; } = "INFO";

    [BsonElement("message")]
    public string Message { get; set; } = "";
}
