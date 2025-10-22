using AutoMapper;
using Microsoft.Extensions.Logging;
using SimQ.Core.Convertors.Problem;
using SimQ.Core.Dtos.In;
using SimQ.Core.Dtos.Out;
using SimQ.Core.Factories.Base;
using SimQ.Core.Models.Base;
using SimQ.DAL.Repository;
using SimQ.Domain.Models.Base;
using SimQ.Domain.Models.ProblemAggregation;
using SimQ.Domain.Models.ResultAggregation;

namespace SimQ.Core.Services;

public interface IProblemService
{
    Task<ProblemResponse?> GetProblemAsync(string problemId, CancellationToken cancellationToken = default);
    Task<ProblemListReponse> GetAllProblemsAsync(CancellationToken cancellationToken = default);
    Task<RegisterProblemResponse?> RegisterProblem(RegisterProblemRequest request, CancellationToken cancellationToken = default);
    Task<ResultListResponse?> GetProblemResults(string problemId, CancellationToken cancellationToken = default);
    Task<ResultDto?> GetResult(string problemId, string resultId, CancellationToken cancellationToken = default);
    Task<bool> DeleteResult(string problemId, string resultId, CancellationToken cancellationToken = default);
    Task<bool> DeleteProblem(string problemId, CancellationToken cancellationToken = default);
}

internal class ProblemService : IProblemService
{
    private readonly IProblemRepository _problemRepository;
    private readonly IResultRepository _resultRepository;
    private readonly IProblemConvertor _problemConvertor;
    private readonly BaseFactory<IModellingAgent> _modellingAgentFactory;
    private readonly IMapper _mapper;
    private readonly ILogger<ProblemService> _logger;


    public ProblemService(
        IProblemRepository problemRepository,
        IResultRepository resultRepository,
        IProblemConvertor problemConvertor,
        BaseFactory<IModellingAgent> modellingAgentFactory,
        IMapper mapper,
        ILogger<ProblemService> logger)
    {
        _problemRepository = problemRepository;
        _resultRepository = resultRepository;
        _problemConvertor = problemConvertor;
        _modellingAgentFactory = modellingAgentFactory;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ProblemResponse?> GetProblemAsync(string id, CancellationToken cancellationToken = default)
    {
        var problem = await GetProblemByIdAsync(id, cancellationToken);
        if (problem == null)
        {
            _logger.LogWarning("Problem with id: {id} was not found", id);
            return null;
        }
        
        return await CreateResponseAsync(problem, cancellationToken);;
    }

    private async Task<ProblemResponse> CreateResponseAsync(Problem problem, CancellationToken cancellationToken)
    {
        var response = new ProblemResponse();

        Models.Problem problemDto;
        try
        {
            problemDto = _problemConvertor.Convert(problem);
        }
        catch(Exception ex)
        {
            throw new Exception($"Problem with id: {problem.Id} cannot be converted.", ex);
        }
        response.ProblemName = problemDto.Name;
        response.Agents = problemDto.Agents;
        
        var results = await GetResultsByProblemIdAsync(problem.Id, cancellationToken);
        if(results?.Count > 0)
            response.Results = results.Select(_mapper.Map<ResultDto>).ToList();

        var links = problemDto.Links?.ToDictionary(
            link => link.Key,
            link => link.Value.Select(agent => agent.Id).ToArray()
        ) ?? new Dictionary<string, string[]>();
            
        if(links.Count > 0)
            response.Links = links;
        
        return response;
    }

    public async Task<ProblemListReponse> GetAllProblemsAsync(CancellationToken cancellationToken = default)
    {
        var problems = await _problemRepository.GetAllAsync(cancellationToken);
        
        var problemResponses = new List<ProblemResponse>();
        
        foreach (var problem in problems)
        {
            var response = await CreateResponseAsync(problem, cancellationToken);
            
            problemResponses.Add(response);
        }

        return new ProblemListReponse
        {
            Data = problemResponses,
            Total = problemResponses.Count
        };
    }

    public async Task<RegisterProblemResponse?> RegisterProblem(RegisterProblemRequest request, CancellationToken cancellationToken = default)
    {
        var agentDtos = request.Agents;
        var agents = agentDtos.Select(_mapper.Map<Agent>)
            .Where(dto => _modellingAgentFactory.Contains(dto.ReflectionType))
            .ToList();

        if (agents.Count == 0)
        {
            _logger.LogWarning($"Агенты {agentDtos} не смогли конвертироваться в Domain.");
            return null;
        }
        
        var problem = new Problem
        {
            Name = request.Name,
            Agents = agents,
            Links = request.Links,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        problem = await _problemRepository.AddAsync(problem, cancellationToken);
        
        return new RegisterProblemResponse
        {
            Id = problem.Id,
            ProblemName = problem.Name
        };
    }

    public async Task<ResultListResponse?> GetProblemResults(string problemId, CancellationToken cancellationToken = default)
    {
        var problem = await GetProblemByIdAsync(problemId);
        if (problem == null)
        {
            return null;
        }
        
        var results = await GetResultsByProblemIdAsync(problem.Id);
        if (results?.Count == 0)
        {
            return null;
        }

        return new ResultListResponse
        {
            Results = results.Select(_mapper.Map<ResultDto>).ToArray()
        };
    }

    public async Task<ResultDto?> GetResult(string problemId, string resultId, CancellationToken cancellationToken = default)
    {
        var result = await GetResultByProblemAndResultIdAsync(problemId, resultId);

        if (result == null)
        {
            return null;
        }

        return new ResultDto
        {
            Text = result.Text,
            CreatedAt = result.CreatedAt,
            TaskId = result.TaskId
        };
    }

    

    public async Task<bool> DeleteResult(string problemId, string resultId, CancellationToken cancellationToken = default)
    {
        var result = await GetResultByProblemAndResultIdAsync(problemId, resultId);

        if (result == null)
        {
            return false;
        }

        return await _resultRepository.DeleteAsync(result.Id);
    }

    public async Task<bool> DeleteProblem(string problemId, CancellationToken cancellationToken = default)
    {
        var problem = await GetProblemByIdAsync(problemId);
        if (problem == null)
        {
            return false;
        }

        // реализовать каскадное удаление 
        await _resultRepository.DeleteResultsByProblemId(problemId, cancellationToken);
        
        var completeDelete = await _problemRepository.DeleteAsync(problem.Id);
        
        return completeDelete;
    }
    
    private async Task<Result?> GetResultByProblemAndResultIdAsync(string problemId, string resultId, CancellationToken cancellationToken = default)
    {
        var resultRequest = new BaseRequest<Result>
        {
            Predicate = r => r.Id == resultId && r.ProblemId == problemId
        };
        
        return await _resultRepository.GetAsync(resultRequest);;
    }

    private async Task<Problem?> GetProblemByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = new BaseRequest<Problem>
        {
            Predicate = problem => problem.Id == id
        };

        return await _problemRepository.GetAsync(request, cancellationToken);
    }

    private async Task<List<Result>?> GetResultsByProblemIdAsync(string problemId, CancellationToken cancellationToken = default)
    {
        var resultsRequest = new BaseRequest<Result>
        {
            Predicate = result => result.ProblemId == problemId
        };
        
        return await _resultRepository.GetManyAsync(resultsRequest, cancellationToken);
    }
}