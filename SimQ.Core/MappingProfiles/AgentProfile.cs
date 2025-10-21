using AutoMapper;
using SimQ.Core.Dtos.In;
using SimQ.Domain.Models.ProblemAggregation;
using Buffer = SimQ.Domain.Models.ProblemAggregation.Buffer;

namespace SimQ.Core.MappingProfiles;

public class AgentProfile : Profile
{
    public AgentProfile()
    {
        CreateMap<AgentDto, Agent>()
            .ConstructUsing((dto, context) =>
            {
                return dto.Type switch
                {
                    AgentType.SERVICE_BLOCK => context.Mapper.Map<ServiceBlock>(dto),
                    AgentType.SOURCE        => context.Mapper.Map<Source>(dto),
                    AgentType.BUFFER        => context.Mapper.Map<Buffer>(dto),
                    _ => throw new NotSupportedException($"Mapping for AgentType '{dto.Type}' is not implemented.")
                };
            })
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.ReflectionType, opt => opt.MapFrom(src => src.ReflectionType))
            .ForMember(dest => dest.Arguments, opt => opt.MapFrom(src => src.Parameters));
        
        CreateMap<AgentDto, ServiceBlock>()
            .ForMember(dest => dest.BindedBuffer, opt => opt.MapFrom(src => src.BindedBuffer));

        CreateMap<AgentDto, Source>();
        CreateMap<AgentDto, Buffer>();
    }
}