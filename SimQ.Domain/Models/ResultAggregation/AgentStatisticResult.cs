using MongoDB.Bson.Serialization.Attributes;
using SimQ.Domain.Models.ProblemAggregation;

namespace SimQ.Domain.Models.ResultAggregation;

public class AgentStatisticResult
{
    [BsonElement("agentId")]
    public string AgentId { get; set; } = string.Empty;

    [BsonElement("agentType")]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public AgentType AgentType { get; set; }

    [BsonElement("average")]
    public double Average { get; set; }

    [BsonElement("statesProbabilities")]
    public Dictionary<string, double> StatesProbabilities { get; set; } = new();
}
