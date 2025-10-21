using AutoMapper;
using SimQ.Core.Convertors.Problem;
using SimQ.Core.Dtos.In;
using SimQ.Core.Dtos.Out;
using SimQ.Core.Factories;
using SimQ.Core.Factories.Base;
using SimQ.DAL.Repository;
using SimQ.Domain.Models.Base;
using SimQ.Domain.Models.ProblemAggregation;
using SimQ.Domain.Models.ResultAggregation;
using SimQCore.Modeller.Models;

namespace SimQ.Core.Services;

public interface IProblemService
{
    Task<ProblemResponse?> GetProblemAsync(string problemId);
    Task<ProblemListReponse> GetAllProblemsAsync();
    Task<RegisterProblemResponse?> RegisterProblem(RegisterProblemRequest request);
    Task<ResultListResponse?> GetProblemResults(string problemId);
    Task<ResultDto?> GetResult(string problemId, string resultId);
    Task<bool> DeleteResult(string problemId, string resultId);
    Task<bool> DeleteProblem(string problemId);
}

internal class ProblemService : IProblemService
{
    private readonly IProblemRepository _problemRepository;
    private readonly IResultRepository _resultRepository;
    private readonly IProblemConvertor _problemConvertor;
    private readonly BaseFactory<IModellingAgent> _modellingAgentFactory;
    private readonly IMapper _mapper;
    
    
    public ProblemService(
        IProblemRepository problemRepository,
        IResultRepository resultRepository,
        IProblemConvertor problemConvertor,
        BaseFactory<IModellingAgent> modellingAgentFactory,
        IMapper mapper)
    {
        _problemRepository = problemRepository;
        _resultRepository = resultRepository;
        _problemConvertor = problemConvertor;
        _modellingAgentFactory = modellingAgentFactory;
        _mapper = mapper;
    }

    public async Task<ProblemResponse?> GetProblemAsync(string id)
    {
        var problem = await GetProblemByIdAsync(id);
        if (problem == null)
        {
            return null;
        }
        
        var problemDto = _problemConvertor.Convert(problem);
        
        var results = await GetResultsByProblemIdAsync(problem.Id);
        
        var links = problemDto.Links.ToDictionary(
            link => link.Key,
            link => link.Value.Select(agent => agent.Id).ToArray()
        );
        
        return new ProblemResponse
        {
            Agents = problemDto.Agents,
            Links = links,
            Results = results.Select(result => _mapper.Map<ResultDto>(result)).ToList(),
        };
    }

    public async Task<ProblemListReponse> GetAllProblemsAsync()
    {
        var problems = await _problemRepository.GetAllAsync();
        
        var problemResponses = new List<ProblemResponse>();
        
        foreach (var problem in problems)
        {
            var problemDto = _problemConvertor.Convert(problem);
        
            var results = await GetResultsByProblemIdAsync(problem.Id);

            var links = problemDto.Links?.ToDictionary(
                link => link.Key,
                link => link.Value.Select(agent => agent.Id).ToArray()
            ) ?? new Dictionary<string, string[]>();

            problemResponses.Add(new ProblemResponse
            {
                Agents = problemDto.Agents,
                Links = links,
                Results = results.Select(_mapper.Map<ResultDto>).ToList()
            });
        }

        return new ProblemListReponse
        {
            Data = problemResponses,
            Total = problemResponses.Count
        };
    }

    public async Task<RegisterProblemResponse?> RegisterProblem(RegisterProblemRequest request)
    {
        var agentDtos = request.Agents;
        var agents = agentDtos.Select(_mapper.Map<Agent>)
            .Where(dto => _modellingAgentFactory.Contains(dto.ReflectionType))
            .ToList();

        if (agents.Count == 0)
        {
            return null;
        }
        
        var problem = new Problem
        {
            Name = request.Name,
            Agents = agents,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        problem = await _problemRepository.AddAsync(problem);
        
        return new RegisterProblemResponse
        {
            Id = problem.Id,
            ProblemName = problem.Name
        };
    }

    public async Task<ResultListResponse?> GetProblemResults(string problemId)
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

    public async Task<ResultDto?> GetResult(string problemId, string resultId)
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

    

    public async Task<bool> DeleteResult(string problemId, string resultId)
    {
        var result = await GetResultByProblemAndResultIdAsync(problemId, resultId);

        if (result == null)
        {
            return false;
        }

        return await _resultRepository.DeleteAsync(result.Id);
    }

    public async Task<bool> DeleteProblem(string problemId)
    {
        var problem = await GetProblemByIdAsync(problemId);
        if (problem == null)
        {
            return false;
        }

        // реализовать каскадное удаление 
        await _resultRepository.DeleteResultsByProblemId(problemId);
        
        var completeDelete = await _problemRepository.DeleteAsync(problem.Id);
        
        return completeDelete;
    }
    
    private async Task<Result?> GetResultByProblemAndResultIdAsync(string problemId, string resultId)
    {
        var resultRequest = new BaseRequest<Result>
        {
            Predicate = r => r.Id == resultId && r.ProblemId == problemId
        };
        
        return await _resultRepository.GetAsync(resultRequest);;
    }

    private async Task<Problem?> GetProblemByIdAsync(string id)
    {
        var request = new BaseRequest<Problem>
        {
            Predicate = problem => problem.Id == id
        };

        return await _problemRepository.GetAsync(request);
    }

    private async Task<List<Result>?> GetResultsByProblemIdAsync(string problemId)
    {
        var resultsRequest = new BaseRequest<Result>
        {
            Predicate = result => result.ProblemId == problemId
        };
        
        return await _resultRepository.GetManyAsync(resultsRequest);
    }
}