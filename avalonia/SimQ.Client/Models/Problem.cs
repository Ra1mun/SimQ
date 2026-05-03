using System.Collections.Generic;
using System.Linq;
using Avalonia;

namespace SimQ.Client.Models;

/// <summary>
/// Runtime model of a queueing-system agent placed on the canvas.
/// </summary>
public sealed class Agent
{
    public string Id { get; set; } = "";
    public AgentKind Kind { get; set; }
    public string Name { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }

    // Source / Orbit
    public DistributionParams ArrivalDistribution { get; set; } = new();

    // ServiceBlock
    public int Channels { get; set; } = 1;
    public DistributionParams ServiceDistribution { get; set; } = new();

    // Buffer
    public string Policy { get; set; } = "FIFO";
    public string Capacity { get; set; } = "Infinity";

    // Constants used to compute edge connector points.
    public const double NodeWidth  = 140;
    public const double NodeHeight = 64;
}

public sealed class Edge
{
    public string Id { get; set; } = "";
    public string From { get; set; } = "";
    public string To { get; set; } = "";

    /// <summary>
    /// Right-middle anchor of the source agent. Set by Problem after agents
    /// are loaded (and on agent move).
    /// </summary>
    public Point StartPoint { get; set; }

    /// <summary>Left-middle anchor of the destination agent.</summary>
    public Point EndPoint { get; set; }
}

public sealed class Problem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public string ModifiedAt { get; set; } = "";
    public ProblemStatus Status { get; set; } = ProblemStatus.Draft;
    public List<Agent> Agents { get; set; } = new();
    public List<Edge> Edges { get; set; } = new();

    /// <summary>
    /// Recomputes <see cref="Edge.StartPoint"/>/<see cref="Edge.EndPoint"/>
    /// from current agent positions. Call after construction or whenever
    /// any agent has moved.
    /// </summary>
    public void RebuildEdgeAnchors()
    {
        var byId = Agents.ToDictionary(a => a.Id);
        foreach (var e in Edges)
        {
            if (!byId.TryGetValue(e.From, out var a)) continue;
            if (!byId.TryGetValue(e.To,   out var b)) continue;
            e.StartPoint = new Point(a.X + Agent.NodeWidth, a.Y + Agent.NodeHeight / 2);
            e.EndPoint   = new Point(b.X,                    b.Y + Agent.NodeHeight / 2);
        }
    }
}
