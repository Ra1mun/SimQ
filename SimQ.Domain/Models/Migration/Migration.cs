using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace SimQ.Domain.Models.Migration;

public interface IMigration
{
    int Version { get; }
    Task ApplyAsync(IMongoDatabase database);
    Task RollbackAsync(IMongoDatabase database);
}

public class MigrationRecord
{
    [BsonId]
    public int Version { get; set; }
    public string Name { get; set; }
    public DateTime AppliedAt { get; set; }
}