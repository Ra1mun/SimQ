namespace SimQ.Domain.Models.Base;

public interface IMongoObjectEntity
{
    string Id { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}