using SimQ.Core.Factories.Base;
using SimQ.Core.Models.Base;
using Buffer = SimQ.Domain.Models.ProblemAggregation.Buffer;

namespace SimQ.Core.Convertors.Agents;

public interface IBufferConverter
{
    Buffer Convert(BaseBuffer agentModel);
    BaseBuffer Convert(Buffer buffer);
}

internal class BufferConverter : IBufferConverter
{
    private readonly BaseFactory<IModellingAgent> _modellingAgentFactory;

    public BufferConverter(BaseFactory<IModellingAgent> modellingAgentFactory)
    {
        _modellingAgentFactory = modellingAgentFactory;
    }
    public Buffer Convert(BaseBuffer agentModel)
    {
        return new Buffer
        {
            ReflectionType = agentModel.GetType().Name,
            Id = agentModel.Id
        };
    }

    public BaseBuffer Convert(Buffer buffer)
    {
        var agent = _modellingAgentFactory.TryToCreate(buffer.ReflectionType, buffer.Parameters);
        if (agent is not BaseBuffer bufferDto)
        {
            throw new InvalidOperationException("Unable to convert service block");
        }
        
        return bufferDto;
    }
}