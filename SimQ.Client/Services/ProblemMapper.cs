using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SimQ.Client.Models;

namespace SimQ.Client.Services;

/// <summary>
/// Maps the in-memory <see cref="Problem"/> graph to the wire DTOs expected by
/// SimQ.WebApi. Sink agents are dropped — the server enum has no SINK value.
/// </summary>
public static class ProblemMapper
{
    public static RegisterProblemRequest ToRegisterRequest(Problem p)
    {
        var agents = p.Agents
            .Where(a => a.Kind != AgentKind.Sink)
            .Select(ToAgentDto)
            .ToList();

        var links = p.Edges
            .GroupBy(e => e.From)
            .ToDictionary(g => g.Key, g => g.Select(e => e.To).ToArray());

        return new RegisterProblemRequest
        {
            Name = string.IsNullOrWhiteSpace(p.Name) ? "Untitled" : p.Name,
            Agents = agents,
            Links = links,
        };
    }

    private static AgentDto ToAgentDto(Agent a) => new()
    {
        Id = a.Id,
        ReflectionType = ReflectionTypeFor(a),
        Type = ServerTypeFor(a.Kind),
        Parameters = ParametersFor(a),
    };

    private static string ServerTypeFor(AgentKind k) => k switch
    {
        AgentKind.Source       => "SOURCE",
        AgentKind.ServiceBlock => "SERVICE_BLOCK",
        AgentKind.Buffer       => "BUFFER",
        AgentKind.Orbit        => "ORBIT",
        _ => "SOURCE",
    };

    private static string ReflectionTypeFor(Agent a) => a.Kind switch
    {
        AgentKind.Source       => "Source",
        // ServiceBlock subclasses are the only ones implementing IAgentStatistic
        // on the server, so we must use a concrete type or no statistics will
        // be collected and the result table stays empty.
        AgentKind.ServiceBlock => a.Channels > 0 ? "FinServiceBlocks" : "InfServiceBlocks",
        AgentKind.Buffer       => a.Policy == "LIFO" ? "StackBuffer" : "QueueBuffer",
        AgentKind.Orbit        => "Orbit",
        _ => "Source",
    };

    private static AgentParamsDto? ParametersFor(Agent a)
    {
        var args = new List<JsonElement>();
        DistributionParamsDto? dist = null;

        switch (a.Kind)
        {
            case AgentKind.Source:
            case AgentKind.Orbit:
                dist = ToDistribution(a.ArrivalDistribution);
                break;

            case AgentKind.ServiceBlock:
                dist = ToDistribution(a.ServiceDistribution);
                args.Add(Json(a.Channels));
                break;

            case AgentKind.Buffer:
                if (int.TryParse(a.Capacity, out var cap)) args.Add(Json(cap));
                break;
        }

        if (dist == null && args.Count == 0) return null;
        return new AgentParamsDto { Distribution = dist, Arguments = args };
    }

    private static DistributionParamsDto ToDistribution(DistributionParams d)
    {
        var (name, args) = d.Kind switch
        {
            DistributionKind.Exponential     => ("ExponentialDistribution",     new double[] { d.Rate }),
            DistributionKind.Normal          => ("NormalDistribution",          new double[] { d.Mean, d.Std }),
            DistributionKind.Bernoulli       => ("BernoulliDistribution",       new double[] { d.P }),
            DistributionKind.Beta            => ("BetaDistribution",            new double[] { d.A, d.B }),
            DistributionKind.Binomial        => ("BinomialDistribution",        new double[] { d.P, d.N }),
            DistributionKind.Poisson         => ("PoissonDistribution",         new double[] { d.Rate }),
            DistributionKind.Gamma           => ("GammaDistribution",           new double[] { d.K, d.Theta }),
            DistributionKind.Rayleigh        => ("RayleighDistribution",        new double[] { d.Std }),
            DistributionKind.Geometric       => ("GeometricDistibution",        new double[] { d.P }),
            DistributionKind.Pascal          => ("PascalDistribution",          new double[] { d.P, d.R }),
            DistributionKind.Hypergeometric  => ("HypergeometricDistribution",  new double[] { d.BigN, d.N, d.BigK }),
            DistributionKind.F               => ("FDistribution",               new double[] { d.A, d.B }),
            DistributionKind.T               => ("TDistribution",               new double[] { d.A }),
            _ => ("ExponentialDistribution", new double[] { d.Rate }),
        };
        return new DistributionParamsDto
        {
            ReflectionType = name,
            Arguments = args.Select(Json).ToList(),
        };
    }

    private static JsonElement Json(double v)
        => JsonDocument.Parse(v.ToString(System.Globalization.CultureInfo.InvariantCulture)).RootElement.Clone();
    private static JsonElement Json(int v)
        => JsonDocument.Parse(v.ToString(System.Globalization.CultureInfo.InvariantCulture)).RootElement.Clone();
}
