using SimQ.Domain.Models.TaskAggregation;

namespace SimQ.Core.Dtos.Out;

public class SimulationTaskDto {
    public string TaskId { get; set; }
    public SimulationTaskStatus Status { get; set; }
    public DateTime? Started { get; set; }
    public DateTime? Finished { get; set; }
}

public class SimulationTaskListResponse
{
    public SimulationTaskDto[] Tasks { get; set; }
    public int Total { get; set; }
}

public class CreateTaskResponse {
    public string TaskId { get; set; }
}



