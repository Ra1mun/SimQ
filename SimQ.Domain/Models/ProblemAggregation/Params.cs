using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SimQ.Domain.Models.ProblemAggregation;

public interface IProblemParams
{
    List<BsonValue>? Arguments { get; set; }
}

[BsonDiscriminator("params")]
public sealed class AgentParams : IProblemParams
{
    public DistributionParams? Distribution { get; set; }
    
    [BsonElement("arguments")]
    public List<BsonValue>? Arguments { get; set; }
}

[BsonDiscriminator("dist_params")]
public sealed class DistributionParams : IProblemParams
{
    [BsonElement("dest_type")]
    public string ReflectionType { get; set; }
    
    [BsonElement("arguments")]
    public List<BsonValue>? Arguments { get; set; }
}