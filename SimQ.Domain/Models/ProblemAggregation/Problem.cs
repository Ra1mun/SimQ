using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SimQ.Domain.Models.Base;

namespace SimQ.Domain.Models.ProblemAggregation;

public class Problem : IMongoObjectEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    
    [BsonElement("ReflectionType")]
    public string Name { get; set; }
    
    public List<Agent>? Agents { get; set; }
    
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}