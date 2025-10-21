using SimQ.Core.Factories;
using SimQ.Core.Factories.Base;
using SimQ.Domain.Models.ProblemAggregation;
using SimQCore.Modeller.Models;

namespace SimQ.Core.Convertors.Agents;

public interface IServiceBlockConverter
{
    ServiceBlock Convert(BaseServiceBlock agentModel);

    BaseServiceBlock Convert(ServiceBlock serviceBlock);
}

internal class ServiceBlockConverter : IServiceBlockConverter
{
    private readonly IBufferConverter _bufferConverter;
    private readonly BaseFactory<IModellingAgent> _modellingAgentFactory;

    public ServiceBlockConverter(
        IBufferConverter bufferConverter,
        BaseFactory<IModellingAgent> modellingAgentFactory)
    {
        _bufferConverter = bufferConverter;
        _modellingAgentFactory = modellingAgentFactory;
    }

    public ServiceBlock Convert(BaseServiceBlock serviceBlockDto)
    {
        var serviceBlock = new ServiceBlock
        {
            ReflectionType = serviceBlockDto.GetType().Name,
            Id = serviceBlockDto.Id,
        };

        if (serviceBlockDto.BindedBuffers.Count > 0)
        {
            foreach (var bufferDto in serviceBlockDto.BindedBuffers)
            {
                var buffer = _bufferConverter.Convert(bufferDto);
                serviceBlock.BindBuffer(buffer);
            }
        }

        return serviceBlock;
    }

    public BaseServiceBlock Convert(ServiceBlock serviceBlock)
    {
        var agent = _modellingAgentFactory.TryToCreate(serviceBlock.ReflectionType, serviceBlock.Arguments);
        if (agent is not BaseServiceBlock serviceBlockDto)
        {
            throw new InvalidOperationException("Unable to convert service block");
        }

        if (serviceBlock.BindedBuffer.Count == 0)
            return serviceBlockDto;
        
        var bufferDtos = serviceBlock.BindedBuffer
            .Select(buffer => _bufferConverter.Convert(buffer))
            .OfType<BaseBuffer>()
            .ToList();

        serviceBlockDto.BindedBuffers = bufferDtos;

        return serviceBlockDto;
    }
}