using AutoMapper;
using Microsoft.Extensions.Logging;
using SimQ.Core.Dtos.Out;
using SimQ.DAL.Repository;
using SimQ.Domain.Models.Base;
using SimQ.Domain.Models.ResultAggregation;

namespace SimQ.Core.Services;

public interface IResultService
{
    Task<ResultListResponse> GetAllResultsAsync(string? problemId = null, string? taskId = null, CancellationToken cancellationToken = default);
    Task<ResultDto?> GetResultAsync(string resultId, CancellationToken cancellationToken = default);
    Task<bool> DeleteResultAsync(string resultId, CancellationToken cancellationToken = default);
}

internal class ResultService : IResultService
{
    private readonly IResultRepository _resultRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ResultService> _logger;

    public ResultService(
        IResultRepository resultRepository,
        IMapper mapper,
        ILogger<ResultService> logger)
    {
        _resultRepository = resultRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ResultListResponse> GetAllResultsAsync(string? problemId = null, string? taskId = null, CancellationToken cancellationToken = default)
    {
        List<Result> results;

        if (!string.IsNullOrEmpty(problemId))
        {
            var request = new BaseRequest<Result> { Predicate = r => r.ProblemId == problemId };
            results = await _resultRepository.GetManyAsync(request, cancellationToken) ?? new List<Result>();
        }
        else if (!string.IsNullOrEmpty(taskId))
        {
            var request = new BaseRequest<Result> { Predicate = r => r.TaskId == taskId };
            results = await _resultRepository.GetManyAsync(request, cancellationToken) ?? new List<Result>();
        }
        else
        {
            results = await _resultRepository.GetAllAsync(cancellationToken);
        }

        return new ResultListResponse
        {
            Results = results.Select(r => _mapper.Map<ResultDto>(r)).ToArray()
        };
    }

    public async Task<ResultDto?> GetResultAsync(string resultId, CancellationToken cancellationToken = default)
    {
        var request = new BaseRequest<Result> { Predicate = r => r.Id == resultId };
        var result = await _resultRepository.GetAsync(request, cancellationToken);
        if (result == null)
        {
            _logger.LogWarning("Result with id: {id} was not found", resultId);
            return null;
        }

        return _mapper.Map<ResultDto>(result);
    }

    public async Task<bool> DeleteResultAsync(string resultId, CancellationToken cancellationToken = default)
    {
        return await _resultRepository.DeleteAsync(resultId, cancellationToken);
    }
}
