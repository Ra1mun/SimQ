using Microsoft.Extensions.Options;
using SimQ.Domain.Models.DBSettings;
using SimQ.Domain.Models.ProblemAggregation;

namespace SimQ.DAL.Repository;

public interface IProblemRepository : IBaseRepository<Problem>;

internal class ProblemRepository : BaseMongoRepository<Problem>, IProblemRepository
{
    protected override string CollectionName { get; set; } = "Problems";

    public ProblemRepository(IOptions<DatabaseSettings> options) : base(options)
    { }
}