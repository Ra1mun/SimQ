using SimQ.Core.Convertors.Agents;
using SimQ.Core.Models;
using SimQ.Core.Models.Base;
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

        var agentLookup = agents
            .Where(a => a != null)
            .ToDictionary(a => a!.Id, a => a!);

        var links = new Dictionary<string, List<IModellingAgent>>();
        if (problem.Links != null)
        {
            foreach (var kvp in problem.Links)
            {
                if (!agentLookup.ContainsKey(kvp.Key))
                    continue;

                var targets = kvp.Value
                    .Where(id => agentLookup.ContainsKey(id))
                    .Select(id => agentLookup[id])
                    .ToList();

                links[kvp.Key] = targets;
            }
        }

        var result = new ProblemDto
        {
            CreateAt = problem.CreatedAt,
            Agents = agents,
            Name = problem.Name,
            Links = links
        };

        foreach (var agent in agents)
        {
            if (agent is IAgentStatistic)
                result.AddAgentForStatistic(agent);
        }

        return result;
    }
}