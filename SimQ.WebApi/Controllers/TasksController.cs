using Microsoft.AspNetCore.Mvc;
using SimQ.Core.Dtos.In;
using SimQ.Core.Dtos.Out;
using SimQ.Core.Services;

namespace SimQ.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class TasksController : ControllerBase
{
    private readonly ILogger<TasksController> _logger;
    private readonly ITaskService _taskService;

    public TasksController(ILogger<TasksController> logger,
        ITaskService taskService)
    {
        _logger = logger;
        _taskService = taskService;
    }

    [HttpGet("tasks")]
    public async Task<ActionResult<SimulationTaskListResponse>> GetTaskList()
    {
        var tasks = await _taskService.GetAllTasks();
        if (tasks.Total == 0) return NoContent();
        return tasks;
    }

    [HttpGet("task/{taskId:guid}")]
    public async Task<ActionResult<SimulationTaskDto>> GetTaskInfo([FromRoute] Guid taskId)
    {
        _logger.LogError($"GetTaskInfo, task_id: {ModelState.IsValid}");
        var task = await _taskService.GetTask(taskId.ToString());
        if (task == null) return NotFound($"Task with id: {taskId} not found");

        return task;
    }

    [HttpPost("task")]
    public async Task<ActionResult<CreateTaskResponse>> CreateTask(
        [FromBody] CreateTaskRequest request
    )
    {
        var task = await _taskService.AddTask(request);
        if (task == null) return BadRequest();

        return task;
    }

    [HttpPost("task/{taskId:guid}")]
    public async Task<IActionResult> StopTask([FromRoute] Guid taskId)
    {
        var result = await _taskService.StopTask(taskId.ToString());
        if (!result) return BadRequest();

        return Ok();
    }
}