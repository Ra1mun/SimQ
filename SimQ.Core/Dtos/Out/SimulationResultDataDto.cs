using SimQ.Domain.Models.ProblemAggregation;

namespace SimQ.Core.Dtos.Out;

public class SimulationResultDataDto
{
    public double EndRealTime { get; set; }
    public double MaxRealTime { get; set; }
    public double CurrentEventsAmount { get; set; }
    public double MaxEventsAmount { get; set; }
    public double CurrentModelationTime { get; set; }
    public double MaxModelationTime { get; set; }
    public double CurrentGenerationError { get; set; }
    public double MinGenerationError { get; set; }
    public int TotalCalls { get; set; }
    public List<AgentStatisticResultDto> AgentResults { get; set; } = new();
}

public class AgentStatisticResultDto
{
    public string AgentId { get; set; } = string.Empty;
    public AgentType AgentType { get; set; }
    public double Average { get; set; }
    public Dictionary<int, double> StatesProbabilities { get; set; } = new();
}
