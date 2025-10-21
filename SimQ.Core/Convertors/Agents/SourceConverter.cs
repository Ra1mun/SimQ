using SimQ.Core.Factories;
using SimQ.Core.Factories.Base;
using SimQ.Domain.Models.ProblemAggregation;
using SimQCore.Modeller.Models;

namespace SimQ.Core.Convertors.Agents;

public interface ISourceConverter
{
    Source Convert(BaseSource agentModel);
    BaseSource Convert(Source source);
}

internal class SourceConverter : ISourceConverter
{
    private readonly BaseFactory<IModellingAgent> _modellingAgentFactory;

    public SourceConverter(BaseFactory<IModellingAgent> modellingAgentFactory)
    {
        _modellingAgentFactory = modellingAgentFactory;
    }
    
    public Source Convert(BaseSource agentModel)
    {
        return new Source
        {
            ReflectionType = agentModel.GetType().Name,
            Id = agentModel.Id
        };
    }

    public BaseSource Convert(Source source)
    {
        var agent = _modellingAgentFactory.TryToCreate(source.ReflectionType, source.Arguments);
        if (agent is not BaseSource sourceDto)
        {
            throw new InvalidOperationException("Unable to convert service block");
        }
        
        return sourceDto;
    }
}