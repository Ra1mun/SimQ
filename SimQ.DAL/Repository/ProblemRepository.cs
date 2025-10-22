using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SimQ.DAL.Repository.Base;
using SimQ.Domain.Models.DBSettings;
using SimQ.Domain.Models.ProblemAggregation;

namespace SimQ.DAL.Repository;

public interface IProblemRepository : IBaseRepository<Problem>;

internal class ProblemRepository : BaseMongoRepository<Problem>, IProblemRepository
{
    public ProblemRepository(IMongoDatabase database) : base(database)
    {
    }

    protected override string CollectionName { get; set; } = "Problems";
    
}