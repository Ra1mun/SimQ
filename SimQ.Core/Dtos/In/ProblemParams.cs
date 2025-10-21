using System.Text.Json;

namespace SimQ.Core.Dtos.In;

public interface IProblemParamsDto
{
    List<JsonElement> Arguments { get; set; }
}

public sealed class AgentParamsDto : IProblemParamsDto
{
    public DistributionParamsDto? Distribution { get; set; }
    public List<JsonElement> Arguments { get; set; }
}

public sealed class DistributionParamsDto : IProblemParamsDto
{
    public string ReflectionType { get; set; }
    public List<JsonElement> Arguments { get; set; }
}