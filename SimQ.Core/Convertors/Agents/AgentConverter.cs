using SimQ.Core.Models.Base;
using SimQ.Domain.Models.ProblemAggregation;
using Buffer = SimQ.Domain.Models.ProblemAggregation.Buffer;

namespace SimQ.Core.Convertors.Agents;

public interface IAgentConverter
{ 
    List<IModellingAgent?> ConvertMany(List<Agent>? agents);
    
    List<Agent> ConvertMany(List<IModellingAgent> agents);
}

internal class AgentConverter : IAgentConverter
{
    private readonly IServiceBlockConverter _serviceBlockConverter;
    private readonly ISourceConverter _sourceConverter;
    private readonly IBufferConverter _bufferConverter;

    public AgentConverter(
        IServiceBlockConverter serviceBlockConverter,
        ISourceConverter sourceConverter,
        IBufferConverter bufferConverter)
    {
        _serviceBlockConverter = serviceBlockConverter;
        _sourceConverter = sourceConverter;
        _bufferConverter = bufferConverter;
    }

    public List<IModellingAgent?> ConvertMany(List<Agent>? agents)
    {
        if(agents == null)
            return [];
        
        return agents.Select(Convert).ToList();
    }

    public List<Agent> ConvertMany(List<IModellingAgent> agents)
    {
        return agents.Select(Convert).ToList();
    }

    public Agent Convert(IModellingAgent agentModel)
    {
        return agentModel switch
        {
            BaseServiceBlock serviceBlockDto => _serviceBlockConverter.Convert(serviceBlockDto),
            BaseSource sourceDto => _sourceConverter.Convert(sourceDto),
            BaseBuffer bufferDto => _bufferConverter.Convert(bufferDto),
            _ => throw new Exception("Unknown agent type")
        };
    }

    public IModellingAgent Convert(Agent agent)
    {
        IModellingAgent agentDto = agent switch
        {
            ServiceBlock serviceBlock => _serviceBlockConverter.Convert(serviceBlock),
            Source source => _sourceConverter.Convert(source),
            Buffer buffer => _bufferConverter.Convert(buffer),
            _ => throw new ArgumentException($"Unknown agent type: {agent.ReflectionType}")
        };
        
        agentDto.Id = agent.Id;
        return agentDto;
    }
}