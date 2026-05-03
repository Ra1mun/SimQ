using CommunityToolkit.Mvvm.ComponentModel;
using SimQ.Client.Models;

namespace SimQ.Client.ViewModels;

public partial class AgentViewModel : ObservableObject
{
    public Agent Model { get; }

    public AgentViewModel(Agent model)
    {
        Model = model;
        _x = model.X;
        _y = model.Y;
        _name = model.Name;
    }

    [ObservableProperty] private double _x;
    partial void OnXChanged(double value) => Model.X = value;

    [ObservableProperty] private double _y;
    partial void OnYChanged(double value) => Model.Y = value;

    [ObservableProperty] private string _name;
    partial void OnNameChanged(string value) => Model.Name = value;

    public string Id => Model.Id;
    public AgentKind Kind => Model.Kind;
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
}
