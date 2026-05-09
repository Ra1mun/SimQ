using System;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using SimQ.Client.Models;

namespace SimQ.Client.ViewModels;

public partial class MainViewModel
{
    [RelayCommand]
    private void SelectScreen(string? target)
    {
        if (string.IsNullOrEmpty(target)) return;
        if (Enum.TryParse<AppScreen>(target, true, out var s)) Screen = s;
    }

    [RelayCommand]
    private void PickProblem(ProblemViewModel p)
    {
        CurrentProblem = p;
        SelectedAgent  = null;
    }

    [RelayCommand]
    private void SelectAgent(AgentViewModel? a)
    {
        SelectedAgent = a;
        SelectedEdge  = null;
    }

    [RelayCommand]
    private void AddAgent(string? kindStr)
    {
        if (string.IsNullOrEmpty(kindStr)) return;
        if (!Enum.TryParse<AgentKind>(kindStr, true, out var kind)) return;

        var index = CurrentProblem.Agents.Count;
        var id    = $"{kind.ToString().ToLower()[..3]}{index + 1}";
        var sameKindCount = CurrentProblem.Agents.Count(a => a.Kind == kind);

        var agent = new Agent
        {
            Id   = id,
            Kind = kind,
            Name = $"{kind} #{sameKindCount + 1}",
            X    = 100 + index * 60,
            Y    = 200,
        };

        CurrentProblem.Model.Agents.Add(agent);
        var vm = new AgentViewModel(agent);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(AgentViewModel.X) or nameof(AgentViewModel.Y))
                CurrentProblem.Model.RebuildEdgeAnchors();
        };
        CurrentProblem.Agents.Add(vm);
        SelectedAgent = vm;
        ShowToast($"Добавлен: {agent.Name}");
    }

    [RelayCommand]
    private void DeleteAgent(AgentViewModel? a)
    {
        if (a == null) return;
        CurrentProblem.Agents.Remove(a);
        CurrentProblem.Model.Agents.RemoveAll(x => x.Id == a.Id);

        var dead = CurrentProblem.Edges.Where(e => e.From == a.Id || e.To == a.Id).ToList();
        foreach (var e in dead) CurrentProblem.Edges.Remove(e);
        CurrentProblem.Model.Edges.RemoveAll(e => e.From == a.Id || e.To == a.Id);

        if (SelectedAgent == a) SelectedAgent = null;
    }

    [RelayCommand]
    private void AddEdge(string? param)
    {
        // param = "fromId|toId"
        if (string.IsNullOrEmpty(param)) return;
        var parts = param.Split('|');
        if (parts.Length != 2) return;

        var fromId = parts[0];
        var toId   = parts[1];
        if (fromId == toId) return;

        // Prevent duplicate edges
        if (CurrentProblem.Edges.Any(e => e.From == fromId && e.To == toId)) return;

        var edge = new Edge
        {
            Id   = $"e{CurrentProblem.Edges.Count + 1}_{fromId}_{toId}",
            From = fromId,
            To   = toId,
        };
        CurrentProblem.Model.Edges.Add(edge);
        CurrentProblem.Edges.Add(edge);
        CurrentProblem.Model.RebuildEdgeAnchors();
        ShowToast($"Связь: {fromId} → {toId}");
    }

    [RelayCommand]
    private void SelectEdge(Edge? e)
    {
        SelectedEdge = e;
        SelectedAgent = null;
    }

    [RelayCommand]
    private void DeleteSelectedEdge()
    {
        if (SelectedEdge == null) return;
        CurrentProblem.Edges.Remove(SelectedEdge);
        CurrentProblem.Model.Edges.Remove(SelectedEdge);
        SelectedEdge = null;
    }

    [RelayCommand] private void OpenResults() => Screen = AppScreen.Results;
}
