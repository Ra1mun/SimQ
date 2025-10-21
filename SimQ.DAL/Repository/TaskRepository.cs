using Microsoft.Extensions.Options;
using SimQ.Domain.Models.DBSettings;
using SimQ.Domain.Models.TaskAggregation;

namespace SimQ.DAL.Repository;

public interface ITaskRepository : IBaseRepository<SimulationTask>
{ }

internal class TaskRepository : BaseMongoRepository<SimulationTask>, ITaskRepository
{
    public TaskRepository(IOptions<DatabaseSettings> options) : base(options)
    { }

    protected override string CollectionName { get; set; } = "Tasks";
    
}
