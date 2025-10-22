using System.Text.Json.Serialization;

namespace SimQ.Core.Dtos.In;

public record RegisterProblemRequest 
{
    public string Name { get; set; } 
    public List<AgentDto> Agents { get; set; }
    public Dictionary<string, string[]> Links { get; set; }
}

public record RegisterProblemResponse
{
    public string Id { get;set; }
    public string ProblemName { get; set; }
}