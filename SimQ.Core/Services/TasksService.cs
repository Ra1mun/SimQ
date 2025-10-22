using SimQ.Core.Convertors.Problem;
using SimQ.Core.Dtos.In;
using SimQ.Core.Dtos.Out;
using SimQ.Core.Statistic;
using SimQ.DAL.Repository;
using SimQ.Domain.Models.Base;
using SimQ.Domain.Models.TaskAggregation;
using DalTask = SimQ.Domain.Models.TaskAggregation.SimulationTask;
using DalResult = SimQ.Domain.Models.ResultAggregation.Result;
using Problem = SimQ.Domain.Models.ProblemAggregation.Problem;
using Task = System.Threading.Tasks.Task;

namespace SimQ.Core.Services;

public interface ITaskService
{
    Task<SimulationTaskListResponse?> GetAllTasks();
    Task<SimulationTaskDto?> GetTask(string taskId);
    Task<CreateTaskResponse?> AddTask(CreateTaskRequest request);
    Task<bool> StopTask(string taskId);
}

internal class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProblemRepository _problemRepository;
    private readonly IResultRepository _resultRepository;
    private readonly IProblemConvertor _problemConvertor;

    private const int MAX_RUNNING_TASKS = 5;
    private readonly Queue<DalTask> _tasksQueue = new();

    public TaskService(
        ITaskRepository taskRepository,
        IProblemRepository problemRepository,
        IResultRepository resultRepository,
        IProblemConvertor problemConvertor)
    {
        _taskRepository = taskRepository;
        _problemRepository = problemRepository;
        _resultRepository = resultRepository;
        _problemConvertor = problemConvertor;
    }

    public async Task<SimulationTaskListResponse?> GetAllTasks()
    {
        var tasks = await _taskRepository.GetAllAsync();
        
        return new SimulationTaskListResponse
        {
            Tasks = tasks.Select(MapTask).ToArray(),
            Total = tasks.Count
        };
    }

    public async Task<SimulationTaskDto?> GetTask(string taskId)
    {
        var task = await GetTaskByIdAsync(taskId);
        if (task == null)
        {
            return null;
        }

        return new SimulationTaskDto
        {
            TaskId = task.Id,
            Status = task.Status,
            Started = task.StartTime,
            Finished = task.EndTime
        };
    }

    public async Task<CreateTaskResponse?> AddTask(CreateTaskRequest request)
    {
        var newTask = new DalTask
        {
            ProblemId = request.ProblemId,
            Status = SimulationTaskStatus.Waiting,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var runningTasks = await GetRunningTasks();

        if (runningTasks?.Count >= MAX_RUNNING_TASKS)
        {
            _tasksQueue.Enqueue(newTask);
        }
        else
        {
            _ = RunTaskWithCallback(newTask);
        }

        var saveTask = await _taskRepository.AddAsync(newTask);

        return new CreateTaskResponse
        {
            TaskId = saveTask.Id,
        };
    }

    public async Task<bool> StopTask(string taskId)
    {
        var task = await GetTaskByIdAsync(taskId);
        if (task == null)
        {
            return false;
        }

        task.Status = SimulationTaskStatus.Canceled;
        task.EndTime = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        
        return await _taskRepository.UpdateAsync(task.Id, task);
    }

    private async Task RunTaskWithCallback(DalTask simulationTask)
    {
        try
        {
            var problem = await GetProblemByIdAsync(simulationTask.ProblemId);
            if (problem == null)
            {
                return;
            }
            var problemDto = _problemConvertor.Convert(problem);
            
            simulationTask.Status = SimulationTaskStatus.Modelling;
            simulationTask.StartTime = DateTime.UtcNow;
            simulationTask.UpdatedAt = DateTime.UtcNow;
            await _taskRepository.UpdateAsync(simulationTask.Id, simulationTask);
            
            var modeller = new SimulationModeller();
            modeller.Simulate(problemDto);

            var results = $"Информация по симуляционному моделированию задачи \"{problemDto.Name}\":";
            results += $"\nEndRealTime = {modeller.EndRealTime} (Max = {problemDto.MaxRealTime})";
            results += $"\nCurrentEventsAmount = {modeller.dataCollector.CurrentEventsAmount} (Max = {problemDto.MaxEventsAmount})";
            results += $"\nCurrentModelationTime = {modeller.dataCollector.CurrentModelationTime} (Max = {problemDto.MaxModelationTime})";
            results += $"\nCurrentGenerationError = {modeller.dataCollector.CurrentGenerationError:E} " +
                       $"(Min = {problemDto.GenerationErrorSettings.MinGenerationError:E})";

            var statesStat = new StatesStatistic(modeller.dataCollector);
            results += statesStat.EmpDistToString();

            simulationTask.Results = results;
            simulationTask.Status = SimulationTaskStatus.Completed;
            simulationTask.EndTime = DateTime.UtcNow;
            simulationTask.UpdatedAt = DateTime.UtcNow;

            var newResult = new DalResult
            {
                ProblemId = simulationTask.ProblemId,
                ProblemName = simulationTask.ProblemName,
                TaskId = simulationTask.Id,
                Text = results,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _resultRepository.AddAsync(newResult);
            await _taskRepository.UpdateAsync(simulationTask.Id, simulationTask);
        }
        catch (Exception ex)
        {
            // Handle error
            simulationTask.Status = SimulationTaskStatus.Error;
            simulationTask.EndTime = DateTime.UtcNow;
            simulationTask.UpdatedAt = DateTime.UtcNow;
            simulationTask.Results = $"Error: {ex.Message}";
            await _taskRepository.UpdateAsync(simulationTask.Id, simulationTask);
        }
        finally
        {
            if (_tasksQueue.Count > 0)
            {
                var nextTask = _tasksQueue.Dequeue();
                _ = RunTaskWithCallback(nextTask);
            }
        }
    }
    
    private async Task<DalTask?> GetTaskByIdAsync(string taskId)
    {
        var taskRequest = new BaseRequest<DalTask>
        {
            Predicate = t => t.Id == taskId
        };
        
        return await _taskRepository.GetAsync(taskRequest);;
    }
    
    private Task<List<DalTask>?> GetRunningTasks()
    {
        var runningTasksRequest = new BaseRequest<DalTask>
        {
            Predicate = t => t.Status == SimulationTaskStatus.Modelling
        };
        
        return _taskRepository.GetManyAsync(runningTasksRequest);;
    }
    
    private async Task<Problem?> GetProblemByIdAsync(string id)
    {
        var request = new BaseRequest<Problem>
        {
            Predicate = problem => problem.Id == id
        };

        return await _problemRepository.GetAsync(request);
    }

    private static SimulationTaskDto MapTask(DalTask task)
    {
        return new SimulationTaskDto
        {
            TaskId = task.Id,
            Status = task.Status,
            Started = task.StartTime,
            Finished = task.EndTime
        };
    }
}
