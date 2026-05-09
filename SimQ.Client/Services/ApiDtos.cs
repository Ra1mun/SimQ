using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SimQ.Client.Services;

// ----- Mirror of server DTOs (only fields we actually need on the wire). -----

public sealed class RegisterProblemRequest
{
    public string Name { get; set; } = "";
    public List<AgentDto> Agents { get; set; } = new();
    public Dictionary<string, string[]> Links { get; set; } = new();
}

public sealed class RegisterProblemResponse
{
    public string? Id { get; set; }
    public string? ProblemName { get; set; }
}

public sealed class AgentDto
{
    public string Id { get; set; } = "";
    public string ReflectionType { get; set; } = "";
    public string Type { get; set; } = "";          // SOURCE / SERVICE_BLOCK / BUFFER / ORBIT
    public List<AgentDto>? BindedBuffer { get; set; }
    public AgentParamsDto? Parameters { get; set; }
}

public sealed class AgentParamsDto
{
    public DistributionParamsDto? Distribution { get; set; }
    public List<JsonElement> Arguments { get; set; } = new();
}

public sealed class DistributionParamsDto
{
    public string ReflectionType { get; set; } = "";
    public List<JsonElement> Arguments { get; set; } = new();
}

public sealed class ProblemListResponseDto
{
    public List<ProblemResponseDto> Data { get; set; } = new();
    public int Total { get; set; }
}

public sealed class ProblemResponseDto
{
    public string? Id { get; set; }
    public string? ProblemName { get; set; }
    public List<ServerAgentDto>? Agents { get; set; }
    public List<ServerAgentDto>? DomainAgents { get; set; }
    public Dictionary<string, string[]>? Links { get; set; }
}

/// <summary>
/// Slim view of an agent as returned by the server. The wire shape comes from
/// <c>IModellingAgent</c> so only <see cref="Id"/> and <see cref="Type"/> are
/// guaranteed; coordinates are not stored on the server and are reconstructed
/// on the client via auto-layout.
/// </summary>
public sealed class ServerAgentDto
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string? ReflectionType { get; set; }
    public AgentParamsDto? Parameters { get; set; }
    public List<ServerAgentDto>? BindedBuffer { get; set; }
}

public sealed class CreateTaskRequestDto
{
    public string ProblemId { get; set; } = "";
    public uint? MaxSteps { get; set; }
    public uint? MaxTime { get; set; }
}

public sealed class CreateTaskResponseDto
{
    public string TaskId { get; set; } = "";
}

public sealed class SimulationTaskDto
{
    public string TaskId { get; set; } = "";
    public string Status { get; set; } = "";        // Waiting/Modelling/Error/Canceled/Completed
    public DateTime? Started { get; set; }
    public DateTime? Finished { get; set; }
    public SimulationResultDataDto? ResultData { get; set; }
}

public sealed class SimulationTaskListResponseDto
{
    public SimulationTaskDto[] Tasks { get; set; } = Array.Empty<SimulationTaskDto>();
    public int Total { get; set; }
}

public sealed class SimulationResultDataDto
{
    public double EndRealTime { get; set; }
    public double MaxRealTime { get; set; }
    public double CurrentEventsAmount { get; set; }
    public double MaxEventsAmount { get; set; }
    public double CurrentModelationTime { get; set; }
    public double MaxModelationTime { get; set; }
    public int TotalCalls { get; set; }
    public List<AgentStatisticResultDto> AgentResults { get; set; } = new();
    public List<SimulationLogEntryDto> Logs { get; set; } = new();
}

public sealed class SimulationLogEntryDto
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "INFO";
    public string Message { get; set; } = "";
}

public sealed class AgentStatisticResultDto
{
    public string AgentId { get; set; } = "";
    public string AgentType { get; set; } = "";
    public double Average { get; set; }
    public Dictionary<string, double> StatesProbabilities { get; set; } = new();
}

public sealed class ResultDto
{
    public string? Id { get; set; }
    public string? ProblemId { get; set; }
    public string? ProblemName { get; set; }
    public string? Text { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? TaskId { get; set; }
    public SimulationResultDataDto? Data { get; set; }
}

public sealed class ResultListResponseDto
{
    public ResultDto[] Results { get; set; } = Array.Empty<ResultDto>();
}
