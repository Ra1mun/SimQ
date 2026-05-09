using System.Text.Json.Serialization;
using SimQ.Core.Dtos.In;
using SimQ.Core.Models.Base;

namespace SimQ.Core.Dtos.Out;

public class ProblemResponse
{
    public string Id { get; set; }
    public string ProblemName { get; set; }
    [JsonIgnore]
    public List<IModellingAgent> Agents { get; set; }
    /// <summary>
    /// Domain agents mapped to wire DTOs with clean JSON-serialisable parameters.
    /// </summary>
    public List<AgentDto>? DomainAgents { get; set; }
    public Dictionary<string, string[]>? Links { get; set; }
    public List<ResultDto>? Results { get; set; }
}

public class ProblemListReponse
{
    public List<ProblemResponse> Data { get; set; }
    public int Total { get; set; } // Сделать отдельный запрос, где можно получить общее количество
}