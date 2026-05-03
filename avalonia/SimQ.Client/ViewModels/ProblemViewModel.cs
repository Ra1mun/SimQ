using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SimQ.Client.Models;

namespace SimQ.Client.ViewModels;

public partial class ProblemViewModel : ObservableObject
{
    public Problem Model { get; }

    public ProblemViewModel(Problem model)
    {
        Model = model;
        model.RebuildEdgeAnchors();
        Agents = new ObservableCollection<AgentViewModel>(model.Agents.Select(a => new AgentViewModel(a)));
        Edges  = new ObservableCollection<Edge>(model.Edges);

        foreach (var avm in Agents)
            avm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(AgentViewModel.X) or nameof(AgentViewModel.Y))
                    Model.RebuildEdgeAnchors();
            };
    }

    public string Id          => Model.Id;
    public string Name        => Model.Name;
    public string Description => Model.Description;
    public ProblemStatus Status => Model.Status;

    public ObservableCollection<AgentViewModel> Agents { get; }
    public ObservableCollection<Edge> Edges { get; }

    public int AgentCount  => Agents.Count;
    public int EdgeCount   => Edges.Count;
    public int SourceCount => Agents.Count(a => a.Kind == AgentKind.Source);
    public int BlockCount  => Agents.Count(a => a.Kind == AgentKind.ServiceBlock);
    public int BufferCount => Agents.Count(a => a.Kind == AgentKind.Buffer);

    public string AgentsLabel => $"{Agents.Count} агентов";
}
