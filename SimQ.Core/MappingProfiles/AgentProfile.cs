using AutoMapper;
using MongoDB.Bson;
using SimQ.Core.Dtos.In;
using SimQ.Core.Extensions;
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
                    AgentType.SOURCE => context.Mapper.Map<Source>(dto),
                    AgentType.BUFFER => context.Mapper.Map<Buffer>(dto),
                    _ => throw new NotSupportedException($"Mapping for AgentType '{dto.Type}' is not implemented.")
                };
            });

        CreateMap<AgentParamsDto, AgentParams>()
            .ForMember(dest => dest.Arguments, opt => opt.MapFrom(src => src.Arguments.AsBsonValue()));

        CreateMap<DistributionParamsDto, DistributionParams>()
            .ForMember(dest => dest.Arguments, opt => opt.MapFrom(src => src.Arguments.AsBsonValue()));

        CreateMap<AgentDto, ServiceBlock>();
        CreateMap<AgentDto, Source>();
        CreateMap<AgentDto, Buffer>();
    }
}