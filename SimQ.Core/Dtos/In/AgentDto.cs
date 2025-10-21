using SimQ.Domain.Models.ProblemAggregation;

namespace SimQ.Core.Dtos.In;

public class AgentDto
{
    public string Id { get; set; }
    public string ReflectionType { get; set; }
    public AgentType Type { get; set; }
    public List<AgentDto> BindedBuffer { get; set; } = new();
    public object[] Parameters { get; set; }
}