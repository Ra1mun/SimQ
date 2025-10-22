using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SimQ.DAL.Repository.Base;
using SimQ.Domain.Models.DBSettings;
using SimQ.Domain.Models.TaskAggregation;

namespace SimQ.DAL.Repository;

public interface ITaskRepository : IBaseRepository<SimulationTask>
{ }

internal class TaskRepository : BaseMongoRepository<SimulationTask>, ITaskRepository
{
    public TaskRepository(IMongoDatabase database) : base(database)
    {
    }

    protected override string CollectionName { get; set; } = "Tasks";
    
}
