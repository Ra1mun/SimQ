namespace SimQ.Core.Dtos.Out;

public class ResultDto
{
    public string? Id { get; set; }
    public string? ProblemId { get; set; }
    public string? ProblemName { get; set; }
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TaskId { get; set; }
    public SimulationResultDataDto? Data { get; set; }
}

public class ResultListResponse
{
    public ResultDto[] Results { get; set; }
}