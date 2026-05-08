using System.Collections.Generic;
using System.Linq;
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

        if (dto.Agents != null)
        {
            // Counters per kind for generating readable names like "Source #1".
            var kindCounters = new Dictionary<AgentKind, int>();

            foreach (var a in dto.Agents)
            {
                var kind = ParseKind(a.Type);
                kindCounters.TryGetValue(kind, out var idx);
                kindCounters[kind] = ++idx;

                problem.Agents.Add(new Agent
                {
                    Id = a.Id,
                    Kind = kind,
                    Name = $"{ReadableName(kind)} #{idx}",
                });
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
