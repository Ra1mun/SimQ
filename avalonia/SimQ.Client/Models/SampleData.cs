using System.Collections.Generic;
using System.Linq;

namespace SimQ.Client.Models;

/// <summary>
/// Sample data set for the SimQ client. Mirrors three example problems from
/// the report, including the Figure 1.4 FinServiceProblem.
/// </summary>
public static class SampleData
{
    public static List<Problem> CreateProblems()
    {
        var p1 = new Problem
        {
            Id = "p-001", Name = "FinServiceProblem",
            Description = "Два входящих потока, один блок приборов с LIFO-хранилищем.",
            CreatedAt = "2026-04-15 10:24", ModifiedAt = "2026-04-19 11:02",
            Status = ProblemStatus.Draft,
            Agents = new()
            {
                new Agent {
                    Id = "a1", Kind = AgentKind.Source, Name = "Source #1", X = 60, Y = 120,
                    ArrivalDistribution = new() { Kind = DistributionKind.Bernoulli, P = 0.2 }
                },
                new Agent {
                    Id = "a2", Kind = AgentKind.Source, Name = "Source #2", X = 60, Y = 280,
                    ArrivalDistribution = new() { Kind = DistributionKind.Beta, A = 0.2, B = 0.3 }
                },
                new Agent {
                    Id = "a3", Kind = AgentKind.Buffer, Name = "StackBuffer #1", X = 360, Y = 200,
                    Policy = "LIFO", Capacity = "Infinity"
                },
                new Agent {
                    Id = "a4", Kind = AgentKind.ServiceBlock, Name = "ServiceBlock #1", X = 620, Y = 200,
                    Channels = 1,
                    ServiceDistribution = new() { Kind = DistributionKind.M, Rate = 0.3 }
                },
                new Agent { Id = "a5", Kind = AgentKind.Sink, Name = "Sink", X = 900, Y = 200 },
            },
            Edges = new()
            {
                new Edge { Id="e1", From="a1", To="a3" },
                new Edge { Id="e2", From="a2", To="a3" },
                new Edge { Id="e3", From="a3", To="a4" },
                new Edge { Id="e4", From="a4", To="a5" },
            }
        };

        var p2 = new Problem
        {
            Id = "p-002", Name = "MultiChannelQueue",
            Description = "M/M/3/10 — многоканальная система с ограниченной очередью.",
            CreatedAt = "2026-04-10 14:10", ModifiedAt = "2026-04-18 09:30",
            Status = ProblemStatus.Ready,
            Agents = new()
            {
                new Agent { Id = "b1", Kind = AgentKind.Source, Name = "Source", X = 60, Y = 200,
                    ArrivalDistribution = new() { Kind = DistributionKind.M, Rate = 0.5 } },
                new Agent { Id = "b2", Kind = AgentKind.Buffer, Name = "QueueBuffer", X = 340, Y = 200,
                    Policy = "FIFO", Capacity = "10" },
                new Agent { Id = "b3", Kind = AgentKind.ServiceBlock, Name = "ServiceBlock #1", X = 620, Y = 120,
                    Channels = 3, ServiceDistribution = new() { Kind = DistributionKind.M, Rate = 0.4 } },
                new Agent { Id = "b4", Kind = AgentKind.Sink, Name = "Sink", X = 900, Y = 200 },
            },
            Edges = new()
            {
                new Edge { Id="e1", From="b1", To="b2" },
                new Edge { Id="e2", From="b2", To="b3" },
                new Edge { Id="e3", From="b3", To="b4" },
            }
        };

        var p3 = new Problem
        {
            Id = "p-003", Name = "RetrialOrbitSystem",
            Description = "Система с орбитой повторных заявок.",
            CreatedAt = "2026-04-02 16:40", ModifiedAt = "2026-04-02 16:40",
            Status = ProblemStatus.Draft,
            Agents = new()
            {
                new Agent { Id = "c1", Kind = AgentKind.Source, Name = "Source", X = 60, Y = 200,
                    ArrivalDistribution = new() { Kind = DistributionKind.M, Rate = 0.3 } },
                new Agent { Id = "c2", Kind = AgentKind.ServiceBlock, Name = "ServiceBlock", X = 380, Y = 140,
                    Channels = 2, ServiceDistribution = new() { Kind = DistributionKind.M, Rate = 0.5 } },
                new Agent { Id = "c3", Kind = AgentKind.Orbit, Name = "Orbit #1", X = 380, Y = 320,
                    ArrivalDistribution = new() { Kind = DistributionKind.M, Rate = 0.1 } },
                new Agent { Id = "c4", Kind = AgentKind.Sink, Name = "Sink", X = 720, Y = 200 },
            },
            Edges = new()
            {
                new Edge { Id="e1", From="c1", To="c2" },
                new Edge { Id="e2", From="c2", To="c3" },
                new Edge { Id="e3", From="c3", To="c2" },
                new Edge { Id="e4", From="c2", To="c4" },
            }
        };

        return new() { p1, p2, p3 };
    }

    public static List<RunRecord> CreateRunHistory() => new()
    {
        new() { Id = "t-017", ProblemId = "p-002", ProblemName = "MultiChannelQueue",  Status = RunStatus.Done,      StartedAt = "2026-04-19 10:42", Duration = "00:04:11", Iterations = 100000 },
        new() { Id = "t-016", ProblemId = "p-001", ProblemName = "FinServiceProblem",  Status = RunStatus.Done,      StartedAt = "2026-04-18 18:14", Duration = "00:02:48", Iterations = 50000  },
        new() { Id = "t-015", ProblemId = "p-001", ProblemName = "FinServiceProblem",  Status = RunStatus.Done,      StartedAt = "2026-04-18 09:22", Duration = "00:02:35", Iterations = 50000  },
        new() { Id = "t-014", ProblemId = "p-003", ProblemName = "RetrialOrbitSystem", Status = RunStatus.Failed,    StartedAt = "2026-04-17 12:08", Duration = "00:00:12", Iterations = 0      },
        new() { Id = "t-013", ProblemId = "p-002", ProblemName = "MultiChannelQueue",  Status = RunStatus.Done,      StartedAt = "2026-04-16 15:55", Duration = "00:06:02", Iterations = 200000 },
        new() { Id = "t-012", ProblemId = "p-001", ProblemName = "FinServiceProblem",  Status = RunStatus.Done,      StartedAt = "2026-04-14 11:31", Duration = "00:02:44", Iterations = 50000  },
        new() { Id = "t-011", ProblemId = "p-002", ProblemName = "MultiChannelQueue",  Status = RunStatus.Cancelled, StartedAt = "2026-04-12 08:19", Duration = "00:01:03", Iterations = 0      },
    };

    public static List<ResultRow> CreateResultTable()
    {
        var raw = new List<(int n, double p)>();
        const double mean = 6.3, sig = 2.4;
        double total = 0;
        for (int n = 0; n < 16; n++)
        {
            var z = (n - mean) / sig;
            var p = System.Math.Exp(-0.5 * z * z);
            raw.Add((n, p));
            total += p;
        }
        var rows = raw.Select(r => new ResultRow { N = r.n, P = r.p / total }).ToList();

        double acc = 0;
        var maxP = rows.Max(r => r.P);
        foreach (var r in rows)
        {
            acc += r.P;
            r.Cdf = acc;
            r.BarWidth    = 220 * (r.P / maxP);
            r.ChartHeight = 200 * (r.P / maxP);
        }
        return rows;
    }
}
