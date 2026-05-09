using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SimQ.Client.Models;

namespace SimQ.Client.Services;

/// <summary>
/// Reconstructs a client-side <see cref="Problem"/> from a server response.
/// The server does not store agent coordinates, so positions are produced by
/// <see cref="AutoLayout"/> based on the agent kind and topology.
/// </summary>
internal static class ProblemFromServer
{
    public static Problem FromDto(ProblemResponseDto dto)
    {
        var problem = new Problem
        {
            Id = dto.Id ?? "",
            Name = dto.ProblemName ?? "Untitled",
            Status = ProblemStatus.Ready,
        };

        // Prefer DomainAgents (carry full Parameters) over Agents (modelling layer, no params)
        var agentDtos = dto.DomainAgents ?? dto.Agents;
        if (agentDtos != null)
        {
            var kindCounters = new Dictionary<AgentKind, int>();

            foreach (var a in agentDtos)
            {
                var kind = ParseKind(a.Type);
                kindCounters.TryGetValue(kind, out var idx);
                kindCounters[kind] = ++idx;

                var agent = new Agent
                {
                    Id = a.Id,
                    Kind = kind,
                    Name = $"{ReadableName(kind)} #{idx}",
                };

                // Restore parameters from server DTO
                if (a.Parameters != null)
                {
                    var dist = a.Parameters.Distribution;
                    if (dist != null)
                    {
                        var dp = ParseDistribution(dist);
                        if (kind is AgentKind.Source or AgentKind.Orbit)
                            agent.ArrivalDistribution = dp;
                        else if (kind == AgentKind.ServiceBlock)
                            agent.ServiceDistribution = dp;
                    }

                    var args = a.Parameters.Arguments;
                    if (args != null && args.Count > 0)
                    {
                        if (kind == AgentKind.ServiceBlock)
                            agent.Channels = GetInt(args, 0, 1);
                        else if (kind == AgentKind.Buffer)
                            agent.Capacity = GetInt(args, 0, 0).ToString();
                    }
                }

                // Restore buffer policy from ReflectionType
                if (kind == AgentKind.Buffer)
                    agent.Policy = a.ReflectionType == "StackBuffer" ? "LIFO" : "FIFO";

                problem.Agents.Add(agent);
            }
        }

        if (dto.Links != null)
        {
            int idx = 0;
            foreach (var kv in dto.Links)
            foreach (var to in kv.Value)
            {
                problem.Edges.Add(new Edge
                {
                    Id = $"e{++idx}_{kv.Key}_{to}",
                    From = kv.Key,
                    To = to,
                });
            }
        }

        AutoLayout(problem);
        problem.RebuildEdgeAnchors();
        return problem;
    }

    private static DistributionParams ParseDistribution(DistributionParamsDto dist)
    {
        var dp = new DistributionParams();
        var args = dist.Arguments ?? new List<JsonElement>();
        double[] vals = args.Select(GetDouble).ToArray();

        (dp.Kind, var _) = dist.ReflectionType switch
        {
            "ExponentialDistribution"    => (DistributionKind.Exponential, Assign(dp, vals, "rate")),
            "NormalDistribution"         => (DistributionKind.Normal, Assign(dp, vals, "mean", "std")),
            "BernoulliDistribution"      => (DistributionKind.Bernoulli, Assign(dp, vals, "p")),
            "BetaDistribution"           => (DistributionKind.Beta, Assign(dp, vals, "a", "b")),
            "BinomialDistribution"       => (DistributionKind.Binomial, Assign(dp, vals, "p", "n")),
            "PoissonDistribution"        => (DistributionKind.Poisson, Assign(dp, vals, "rate")),
            "GammaDistribution"          => (DistributionKind.Gamma, Assign(dp, vals, "k", "theta")),
            "RayleighDistribution"       => (DistributionKind.Rayleigh, Assign(dp, vals, "std")),
            "GeometricDistibution"       => (DistributionKind.Geometric, Assign(dp, vals, "p")),
            "PascalDistribution"         => (DistributionKind.Pascal, Assign(dp, vals, "p", "r")),
            "HypergeometricDistribution" => (DistributionKind.Hypergeometric, Assign(dp, vals, "bigN", "n", "bigK")),
            "FDistribution"              => (DistributionKind.F, Assign(dp, vals, "a", "b")),
            "TDistribution"              => (DistributionKind.T, Assign(dp, vals, "a")),
            _                            => (DistributionKind.Exponential, Assign(dp, vals, "rate")),
        };
        return dp;
    }

    private static bool Assign(DistributionParams dp, double[] vals, params string[] fields)
    {
        for (int i = 0; i < fields.Length && i < vals.Length; i++)
        {
            switch (fields[i])
            {
                case "rate":  dp.Rate  = vals[i]; break;
                case "mean":  dp.Mean  = vals[i]; break;
                case "std":   dp.Std   = vals[i]; break;
                case "p":     dp.P     = vals[i]; break;
                case "a":     dp.A     = vals[i]; break;
                case "b":     dp.B     = vals[i]; break;
                case "k":     dp.K     = vals[i]; break;
                case "theta": dp.Theta = vals[i]; break;
                case "n":     dp.N     = (int)vals[i]; break;
                case "r":     dp.R     = (int)vals[i]; break;
                case "bigN":  dp.BigN  = (int)vals[i]; break;
                case "bigK":  dp.BigK  = (int)vals[i]; break;
            }
        }
        return true;
    }

    private static double GetDouble(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number) return el.GetDouble();
        if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v)) return v;
        return 0;
    }

    private static int GetInt(List<JsonElement> args, int index, int fallback)
    {
        if (index >= args.Count) return fallback;
        var el = args[index];
        if (el.ValueKind == JsonValueKind.Number) return el.GetInt32();
        return fallback;
    }

    private static AgentKind ParseKind(string serverType) => serverType?.ToUpperInvariant() switch
    {
        "SOURCE"        => AgentKind.Source,
        "SERVICE_BLOCK" => AgentKind.ServiceBlock,
        "BUFFER"        => AgentKind.Buffer,
        "ORBIT"         => AgentKind.Orbit,
        _               => AgentKind.Source,
    };

    private static string ReadableName(AgentKind kind) => kind switch
    {
        AgentKind.Source       => "Source",
        AgentKind.ServiceBlock => "ServiceBlock",
        AgentKind.Buffer       => "Buffer",
        AgentKind.Orbit        => "Orbit",
        AgentKind.Sink         => "Sink",
        _                      => "Agent",
    };

    /// <summary>
    /// Places agents into vertical columns by kind: Source → Buffer →
    /// ServiceBlock → Orbit → Sink. Within a column agents are stacked
    /// vertically with a fixed gap. Used when the server returns no
    /// coordinate metadata.
    /// </summary>
    public static void AutoLayout(Problem p)
    {
        const double colGap = 220;
        const double rowGap = 120;
        const double topY   = 120;
        const double leftX  = 60;

        var order = new[]
        {
            AgentKind.Source, AgentKind.Buffer,
            AgentKind.ServiceBlock, AgentKind.Orbit, AgentKind.Sink,
        };

        var byKind = order.ToDictionary(k => k, k => p.Agents.Where(a => a.Kind == k).ToList());
        for (int col = 0; col < order.Length; col++)
        {
            var list = byKind[order[col]];
            for (int row = 0; row < list.Count; row++)
            {
                list[row].X = leftX + col * colGap;
                list[row].Y = topY + row * rowGap;
            }
        }
    }
}
