using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using SimQ.Client.Models;

namespace SimQ.Client.ViewModels;

public partial class AgentViewModel : ObservableObject
{
    public Agent Model { get; }

    public DistributionViewModel ArrivalDistribution { get; }
    public DistributionViewModel ServiceDistribution { get; }

    public IReadOnlyList<string> AllPolicies { get; } = new[] { "FIFO", "LIFO" };

    public AgentViewModel(Agent model)
    {
        Model = model;
        _x = model.X;
        _y = model.Y;
        _name = model.Name;
        _channels = model.Channels;
        _capacity = model.Capacity;
        _policy = model.Policy;
        ArrivalDistribution = new DistributionViewModel(model.ArrivalDistribution, RaiseParamsChanged);
        ServiceDistribution = new DistributionViewModel(model.ServiceDistribution, RaiseParamsChanged);
    }

    [ObservableProperty] private double _x;
    partial void OnXChanged(double value) => Model.X = value;

    [ObservableProperty] private double _y;
    partial void OnYChanged(double value) => Model.Y = value;

    [ObservableProperty] private string _name;
    partial void OnNameChanged(string value) => Model.Name = value;

    [ObservableProperty] private int _channels;
    partial void OnChannelsChanged(int value) { Model.Channels = value; RaiseParamsChanged(); }

    [ObservableProperty] private string _capacity;
    partial void OnCapacityChanged(string value) { Model.Capacity = value; RaiseParamsChanged(); }

    [ObservableProperty] private string _policy;
    partial void OnPolicyChanged(string value) { Model.Policy = value; RaiseParamsChanged(); }

    public string Id => Model.Id;
    public AgentKind Kind => Model.Kind;

    public bool IsSource       => Kind == AgentKind.Source;
    public bool IsServiceBlock => Kind == AgentKind.ServiceBlock;
    public bool IsBuffer       => Kind == AgentKind.Buffer;
    public bool IsOrbit        => Kind == AgentKind.Orbit;
    public bool IsSink         => Kind == AgentKind.Sink;

    public bool HasArrivalDistribution => Kind is AgentKind.Source or AgentKind.Orbit;
    public bool HasServiceDistribution => Kind == AgentKind.ServiceBlock;
    public bool HasChannels            => Kind == AgentKind.ServiceBlock;
    public bool HasBufferOptions       => Kind == AgentKind.Buffer;

    public string ShortLabel => Kind switch
    {
        AgentKind.Source       => "SRC",
        AgentKind.ServiceBlock => "SVB",
        AgentKind.Buffer       => "BUF",
        AgentKind.Orbit        => "ORB",
        AgentKind.Sink         => "SNK",
        _ => "?"
    };

    public string KindLabel => Kind switch
    {
        AgentKind.Source       => "Источник",
        AgentKind.ServiceBlock => "Блок приборов",
        AgentKind.Buffer       => "Хранилище",
        AgentKind.Orbit        => "Орбита",
        AgentKind.Sink         => "Сток",
        _ => Kind.ToString()
    };

    public string ParamsSummary => Kind switch
    {
        AgentKind.Source or AgentKind.Orbit => Model.ArrivalDistribution.Format(),
        AgentKind.ServiceBlock              => $"c={Model.Channels}  {Model.ServiceDistribution.Format()}",
        AgentKind.Buffer                    => $"{Model.Policy}  cap={Model.Capacity}",
        AgentKind.Sink                      => "выход",
        _ => "—"
    };

    private void RaiseParamsChanged() => OnPropertyChanged(nameof(ParamsSummary));
}
