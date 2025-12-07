using SimQ.Core.Dtos;
using SimQ.Core.Dtos.Out;
using SimQ.DAL.Models;
using SimQ.DAL.Repository;
using SimQCore.Modeller;
using SimQCore.Statistic;
using DalTask = SimQ.DAL.Models.TaskAggregation.SimulationTask;
using DalResult = SimQ.DAL.Models.ResultAggregation.Result;
using TaskStatus = SimQ.DAL.Models.TaskAggregation.TaskStatus;
using Task = System.Threading.Tasks.Task;

namespace SimQ.DAL.Services;

public interface ITasksService
{
    Task<TaskListResponse> GetTaskList();
    Task<Core.Dtos.Out.Task> GetTaskInfo(Guid taskId);
    Task<CreateTaskResponse> AddTask(CreateTaskRequest request);
    Task StopTask(uint taskId);
}

public class TasksService : ITasksService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProblemRepository _problemRepository;
    private readonly IResultRepository _resultRepository;
    private readonly IProblemsService _problemsService;
    
    private const int MAX_RUNNING_TASKS = 5;
    private readonly Queue<DalTask> _tasksQueue = new();
    private readonly Lock _lock = new();

    public TasksService(
        ITaskRepository taskRepository,
        IProblemRepository problemRepository,
        IResultRepository resultRepository,
        IProblemsService problemsService)
    {
        _taskRepository = taskRepository;
        _problemRepository = problemRepository;
        _resultRepository = resultRepository;
        _problemsService = problemsService;
    }

    public async Task<TaskListResponse> GetTaskList()
    {
        var tasks = await _taskRepository.GetAllAsync();
        
        return new TaskListResponse
        {
            Tasks = tasks.Select(task => new Core.Dtos.Out.Task
            {
                TaskId = task.Id,
                Status = (Core.Dtos.Out.TaskStatus)task.Status,
                Started = task.StartTime,
                Finished = task.EndTime
            }).ToArray(),
            Total = tasks.Count
        };
    }

    public async Task<Core.Dtos.Out.Task> GetTaskInfo(Guid taskId)
    {
        var taskRequest = new BaseRequest<DalTask>
        {
            Predicate = t => t.Id == taskId
        };
        var task = await _taskRepository.GetAsync(taskRequest);
        
        if (task == null)
        {
            throw new KeyNotFoundException($"Task '{taskId}' not found");
        }

        return new Core.Dtos.Out.Task
        {
            TaskId = task.Id,
            Status = (Core.Dtos.Out.TaskStatus)task.Status,
            Started = task.StartTime,
            Finished = task.EndTime
        };
    }

    public async Task<CreateTaskResponse> AddTask(CreateTaskRequest request)
    {
        lock (_lock)
        {
            var newTask = new DalTask
            {
                ProblemId = request.ProblemId,
                Status = TaskStatus.Waiting,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Check if max running tasks reached
            var runningTasksTask = GetRunningTasks();
            runningTasksTask.Wait();
            var runningTasks = runningTasksTask.Result;

            if (runningTasks.Count >= MAX_RUNNING_TASKS)
            {
                // Add to queue
                _tasksQueue.Enqueue(newTask);
            }
            else
            {
                // Start immediately
                _ = RunTaskWithCallback(newTask);
            }

            // Save task to database
            var saveTask = _taskRepository.AddAsync(newTask);
            saveTask.Wait();

            return new CreateTaskResponse
            {
                TaskId = taskId
            };
        }
    }

    private Task<List<DalTask>> GetRunningTasks()
    {
        var runningTasksRequest = new BaseRequest<DalTask>
        {
            Predicate = t => t.Status == TaskStatus.Modelling
        };
        
        return _taskRepository.GetManyAsync(runningTasksRequest);;
    }

    public async Task StopTask(uint taskId)
    {
        var taskRequest = new BaseRequest<DalTask>
        {
            Predicate = t => t.TaskId == taskId
        };
        var tasks = await _taskRepository.GetAsync(taskRequest);
        var task = tasks.FirstOrDefault();
        
        if (task == null)
        {
            throw new KeyNotFoundException($"Task '{taskId}' not found");
        }

        task.Status = TaskStatus.Canceled;
        task.EndTime = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;
        
        await _taskRepository.UpdateAsync(task.Id, task);
    }

    private async Task RunTaskWithCallback(DalTask simulationTask)
    {
        try
        {
            // Update task status to Modelling
            simulationTask.Status = TaskStatus.Modelling;
            simulationTask.StartTime = DateTime.UtcNow;
            simulationTask.UpdatedAt = DateTime.UtcNow;
            await _taskRepository.UpdateAsync(simulationTask.Id, simulationTask);

            // Get problem
            var problemDto = await _problemsService.GetProblem(simulationTask.ProblemId);
            
            // Run simulation
            var modeller = new SimulationModeller();
            modeller.Simulate(problemDto);

            // Collect results
            var results = $"Информация по симуляционному моделированию задачи \"{problemDto.Name}\":";
            results += $"\nEndRealTime = {modeller.EndRealTime} (Max = {problemDto.MaxRealTime})";
            results += $"\nCurrentEventsAmount = {modeller.dataCollector.CurrentEventsAmount} (Max = {problemDto.MaxEventsAmount})";
            results += $"\nCurrentModelationTime = {modeller.dataCollector.CurrentModelationTime} (Max = {problemDto.MaxModelationTime})";
            results += $"\nCurrentGenerationError = {modeller.dataCollector.CurrentGenerationError:E} " +
                       $"(Min = {problemDto.generationErrorSettings.MinGenerationError:E})";

            var statesStat = new StatesStatistic(modeller.dataCollector);
            results += statesStat.EmpDistToString();

            simulationTask.Results = results;
            simulationTask.Status = TaskStatus.Completed;
            simulationTask.EndTime = DateTime.UtcNow;
            simulationTask.UpdatedAt = DateTime.UtcNow;

            // Save result
            var resultId = Guid.NewGuid();
            var newResult = new DalResult
            {
                ResultId = resultId.ToString(),
                ProblemId = simulationTask.ProblemId,
                ProblemName = simulationTask.ProblemName,
                TaskId = simulationTask.TaskId,
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
            simulationTask.Status = TaskStatus.Error;
            simulationTask.EndTime = DateTime.UtcNow;
            simulationTask.UpdatedAt = DateTime.UtcNow;
            simulationTask.Results = $"Error: {ex.Message}";
            await _taskRepository.UpdateAsync(simulationTask.Id, simulationTask);
        }
        finally
        {
            // Check if there are queued tasks
            lock (_lock)
            {
                if (_tasksQueue.Count > 0)
                {
                    var nextTask = _tasksQueue.Dequeue();
                    _ = RunTaskWithCallback(nextTask);
                }
            }
        }
    }
}
