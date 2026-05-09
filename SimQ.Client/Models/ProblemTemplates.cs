using System;
using System.Collections.Generic;

namespace SimQ.Client.Models;

/// <summary>
/// Identifiers for the typical queueing-system structures the wizard can pre-populate.
/// Order matches the indices used by the wizard's «ШАБЛОН» combobox.
/// </summary>
public enum ProblemTemplate
{
    /// <summary>No agents — user will build the structure from scratch.</summary>
    Empty = 0,

    /// <summary>M/M/1 — single server, no queue.</summary>
    MM1 = 1,

    /// <summary>M/M/c with explicit buffer (queue).</summary>
    MMcQueue = 2,

    /// <summary>M/M/1 with retrial orbit.</summary>
    Orbit = 3,
}

/// <summary>
/// Factory helpers that pre-populate a <see cref="Problem"/> with the agents
/// and edges of a typical queueing-system template.
/// </summary>
public static class ProblemTemplates
{
    /// <summary>
    /// Human-readable description shown on the wizard's «Структура агентов» step.
    /// </summary>
    public static (string Description, string Agents) Describe(ProblemTemplate template) => template switch
    {
        ProblemTemplate.Empty => (
            "Задача будет создана без агентов. Добавить агентов и связи можно в редакторе после создания задачи.",
            "—"
        ),
        ProblemTemplate.MM1 => (
            "Будет создана типовая структура M/M/1: Source → ServiceBlock → Sink.",
            "• Source #1 — экспоненциальный поток λ=0.3\n• ServiceBlock #1 — 1 канал, μ=0.5\n• Sink — выход"
        ),
        ProblemTemplate.MMcQueue => (
            "Будет создана структура M/M/c с очередью: Source → Buffer → ServiceBlock → Sink.",
            "• Source #1 — экспоненциальный поток λ=0.5\n• Queue #1 — буфер, ёмкость 50\n• ServiceBlock #1 — 3 канала, μ=0.4\n• Sink — выход"
        ),
        ProblemTemplate.Orbit => (
            "Будет создана структура с орбитой повторов: заявки, не попавшие в обслуживание, повторяют попытку.",
            "• Source #1 — экспоненциальный поток λ=0.3\n• ServiceBlock #1 — 1 канал, μ=0.5\n• Orbit #1 — повторный поток λ=1.0\n• Sink — выход"
        ),
        _ => (
            "Неизвестный шаблон.",
            "—"
        ),
    };

    /// <summary>
    /// Populates <paramref name="problem"/> with the agents and edges of the given template.
    /// Existing agents/edges are not cleared — pass an empty <see cref="Problem"/>.
    /// </summary>
    public static void Apply(Problem problem, ProblemTemplate template)
    {
        switch (template)
        {
            case ProblemTemplate.Empty:
                return;

            case ProblemTemplate.MM1:
                problem.Agents.AddRange(new[]
                {
                    new Agent { Id = "src1", Kind = AgentKind.Source,       Name = "Source #1",       X =  60, Y = 200,
                                ArrivalDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.3 } },
                    new Agent { Id = "svb1", Kind = AgentKind.ServiceBlock, Name = "ServiceBlock #1", X = 360, Y = 200,
                                Channels = 1,
                                ServiceDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.5 } },
                    new Agent { Id = "snk1", Kind = AgentKind.Sink,         Name = "Sink",            X = 660, Y = 200 },
                });
                problem.Edges.AddRange(new[]
                {
                    new Edge { Id = "e1", From = "src1", To = "svb1" },
                    new Edge { Id = "e2", From = "svb1", To = "snk1" },
                });
                return;

            case ProblemTemplate.MMcQueue:
                problem.Agents.AddRange(new[]
                {
                    new Agent { Id = "src1", Kind = AgentKind.Source,       Name = "Source #1",       X =  60, Y = 200,
                                ArrivalDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.5 } },
                    new Agent { Id = "buf1", Kind = AgentKind.Buffer,       Name = "Queue #1",        X = 240, Y = 200,
                                Capacity = "50" },
                    new Agent { Id = "svb1", Kind = AgentKind.ServiceBlock, Name = "ServiceBlock #1", X = 460, Y = 200,
                                Channels = 3,
                                ServiceDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.4 } },
                    new Agent { Id = "snk1", Kind = AgentKind.Sink,         Name = "Sink",            X = 700, Y = 200 },
                });
                problem.Edges.AddRange(new[]
                {
                    new Edge { Id = "e1", From = "src1", To = "buf1" },
                    new Edge { Id = "e2", From = "buf1", To = "svb1" },
                    new Edge { Id = "e3", From = "svb1", To = "snk1" },
                });
                return;

            case ProblemTemplate.Orbit:
                problem.Agents.AddRange(new[]
                {
                    new Agent { Id = "src1", Kind = AgentKind.Source,       Name = "Source #1",       X =  60, Y = 200,
                                ArrivalDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.3 } },
                    new Agent { Id = "svb1", Kind = AgentKind.ServiceBlock, Name = "ServiceBlock #1", X = 360, Y = 200,
                                Channels = 1,
                                ServiceDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.5 } },
                    new Agent { Id = "orb1", Kind = AgentKind.Orbit,        Name = "Orbit #1",        X = 360, Y = 400,
                                ArrivalDistribution = new() { Kind = DistributionKind.Exponential, Rate = 1.0 } },
                    new Agent { Id = "snk1", Kind = AgentKind.Sink,         Name = "Sink",            X = 660, Y = 200 },
                });
                problem.Edges.AddRange(new[]
                {
                    new Edge { Id = "e1", From = "src1", To = "svb1" },
                    new Edge { Id = "e2", From = "svb1", To = "snk1" },
                    new Edge { Id = "e3", From = "svb1", To = "orb1" },
                    new Edge { Id = "e4", From = "orb1", To = "svb1" },
                });
                return;

            default:
                Apply(problem, ProblemTemplate.MM1);
                return;
        }
    }
}
