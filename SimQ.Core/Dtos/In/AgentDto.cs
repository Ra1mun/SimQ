using System.Text.Json.Serialization;
using SimQ.Domain.Models.ProblemAggregation;

namespace SimQ.Core.Dtos.In;

public class AgentDto
{
    [JsonIgnore]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ReflectionType { get; set; }
    public AgentType Type { get; set; }
    public List<AgentDto>? BindedBuffer { get; set; }
    public AgentParamsDto? Parameters { get; set; }
}