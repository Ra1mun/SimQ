using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SimQ.DAL.Repository.Base;
using SimQ.Domain.Models.DBSettings;
using SimQ.Domain.Models.ResultAggregation;

namespace SimQ.DAL.Repository;

public interface IResultRepository : IBaseRepository<Result>
{
    Task DeleteResultsByProblemId(string problemId, CancellationToken cancellationToken);
}

internal class ResultRepository : BaseMongoRepository<Result>, IResultRepository
{
    public ResultRepository(IOptions<DatabaseSettings> options) : base(options)
    { }

    protected override string CollectionName { get; set; } = "Results";


    public async Task DeleteResultsByProblemId(string problemId, CancellationToken cancellationToken)
    {
        await Collection.DeleteOneAsync(x => x.ProblemId == problemId, cancellationToken);
    }
}
