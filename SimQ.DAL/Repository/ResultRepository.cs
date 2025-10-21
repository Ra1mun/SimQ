using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SimQ.Domain.Models.DBSettings;
using SimQ.Domain.Models.ResultAggregation;

namespace SimQ.DAL.Repository;

public interface IResultRepository : IBaseRepository<Result>
{
    Task DeleteResultsByProblemId(string problemId);
}

internal class ResultRepository : BaseMongoRepository<Result>, IResultRepository
{
    public ResultRepository(IOptions<DatabaseSettings> options) : base(options)
    { }

    protected override string CollectionName { get; set; } = "Results";


    public async Task DeleteResultsByProblemId(string problemId)
    {
        await Collection.DeleteOneAsync(r => r.ProblemId == problemId);
    }
}
