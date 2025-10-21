namespace SimQ.Core.Dtos.Out;

public class ResultDto
{
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TaskId { get; set; }
}

public class ResultListResponse
{
    public ResultDto[] Results { get; set; }
}