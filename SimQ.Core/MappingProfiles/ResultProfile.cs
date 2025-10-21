using AutoMapper;
using SimQ.Core.Dtos.Out;
using SimQ.Domain.Models.ResultAggregation;

namespace SimQ.Core.MappingProfiles;

public class ResultProfile : Profile
{
    public ResultProfile()
    {
        CreateMap<Result, ResultDto>();
        CreateMap<ResultDto, Result>();
    }
}