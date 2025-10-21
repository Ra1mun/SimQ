namespace SimQ.Core.Dtos.In;

public class CreateTaskRequest {
    public string ProblemId { get; set; }
    public uint? MaxSteps { get; set; }
    public uint? MaxTime { get; set; }
}