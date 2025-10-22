using SimQ.Core.Convertors.Agents;
using ProblemDto = SimQ.Core.Models.Problem;

namespace SimQ.Core.Convertors.Problem;

public interface IProblemConvertor
{
    Domain.Models.ProblemAggregation.Problem Convert(ProblemDto dto);
    ProblemDto Convert(Domain.Models.ProblemAggregation.Problem problem);
}

internal class ProblemConvertor : IProblemConvertor
{
    private readonly IAgentConverter _agentConverter;

    public ProblemConvertor(IAgentConverter agentConverter)
    {
        _agentConverter = agentConverter;
    }


    public Domain.Models.ProblemAggregation.Problem Convert(ProblemDto dto)
    {
        var agents = _agentConverter.ConvertMany(dto.Agents);

        return new Domain.Models.ProblemAggregation.Problem
        {
            Agents = agents
        };
    }

    public ProblemDto Convert(Domain.Models.ProblemAggregation.Problem problem)
    {
        var agents = _agentConverter.ConvertMany(problem.Agents);
        
        return new ProblemDto
        {
            CreateAt = problem.CreatedAt,
            Agents = agents,
            Name = problem.Name
        };
    }
}