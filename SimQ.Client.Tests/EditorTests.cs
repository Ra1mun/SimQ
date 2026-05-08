using Avalonia.Headless.XUnit;
using SimQ.Client.Models;
using SimQ.Client.ViewModels;
using SimQ.Client.Views;

namespace SimQ.Client.Tests;

/// <summary>
/// Headless tests for editor-related commands (add/delete agents, pick problem).
/// </summary>
public class EditorTests
{
    private static MainViewModel CreateVm()
    {
        var vm = new MainViewModel(null);
        var window = new MainWindow { DataContext = vm };
        window.Show();
        return vm;
    }

    [AvaloniaFact]
    public void AddAgent_Source_Increases_Agent_Count()
    {
        var vm = CreateVm();
        var before = vm.CurrentProblem.Agents.Count;

        vm.AddAgentCommand.Execute("Source");

        Assert.Equal(before + 1, vm.CurrentProblem.Agents.Count);
    }

    [AvaloniaFact]
    public void AddAgent_Selects_New_Agent()
    {
        var vm = CreateVm();

        vm.AddAgentCommand.Execute("Source");

        Assert.NotNull(vm.SelectedAgent);
        Assert.Equal(AgentKind.Source, vm.SelectedAgent!.Kind);
    }

    [AvaloniaFact]
    public void DeleteAgent_Removes_Agent()
    {
        var vm = CreateVm();
        vm.AddAgentCommand.Execute("Source");
        var added = vm.SelectedAgent!;
        var before = vm.CurrentProblem.Agents.Count;

        vm.DeleteAgentCommand.Execute(added);

        Assert.Equal(before - 1, vm.CurrentProblem.Agents.Count);
        Assert.Null(vm.SelectedAgent);
    }

    [AvaloniaFact]
    public void PickProblem_Changes_CurrentProblem()
    {
        var vm = CreateVm();
        Assert.True(vm.Problems.Count > 1, "Need at least 2 problems for this test");

        var second = vm.Problems[1];
        vm.PickProblemCommand.Execute(second);

        Assert.Equal(second, vm.CurrentProblem);
    }

    [AvaloniaFact]
    public void ShowToast_Sets_HasToast()
    {
        var vm = CreateVm();

        // Trigger a toast via AddAgent (which calls ShowToast internally)
        vm.AddAgentCommand.Execute("ServiceBlock");

        Assert.True(vm.HasToast);
        Assert.False(string.IsNullOrEmpty(vm.Toast));
    }

    [AvaloniaFact]
    public void StartSimulation_Without_Api_Shows_Toast()
    {
        var vm = CreateVm();

        // With null API, StartSimulation should show a toast and not crash
        vm.StartSimulationCommand.Execute(null);

        Assert.False(vm.IsRunning);
        Assert.True(vm.HasToast);
    }

    // ═══════════════════════════════════════════
    //  AddEdge tests
    // ═══════════════════════════════════════════

    [AvaloniaFact]
    public void AddEdge_Creates_Edge_Between_Two_Agents()
    {
        var vm = CreateVm();
        vm.AddAgentCommand.Execute("Source");
        var src = vm.SelectedAgent!;
        vm.AddAgentCommand.Execute("ServiceBlock");
        var svb = vm.SelectedAgent!;
        var before = vm.CurrentProblem.Edges.Count;

        vm.AddEdgeCommand.Execute($"{src.Id}|{svb.Id}");

        Assert.Equal(before + 1, vm.CurrentProblem.Edges.Count);
        var edge = vm.CurrentProblem.Edges.Last();
        Assert.Equal(src.Id, edge.From);
        Assert.Equal(svb.Id, edge.To);
    }

    [AvaloniaFact]
    public void AddEdge_Shows_Toast()
    {
        var vm = CreateVm();
        vm.AddAgentCommand.Execute("Source");
        var src = vm.SelectedAgent!;
        vm.AddAgentCommand.Execute("Sink");
        var snk = vm.SelectedAgent!;

        vm.AddEdgeCommand.Execute($"{src.Id}|{snk.Id}");

        Assert.True(vm.HasToast);
        Assert.Contains("→", vm.Toast);
    }

    [AvaloniaFact]
    public void AddEdge_SelfLoop_Ignored()
    {
        var vm = CreateVm();
        vm.AddAgentCommand.Execute("Source");
        var src = vm.SelectedAgent!;
        var before = vm.CurrentProblem.Edges.Count;

        vm.AddEdgeCommand.Execute($"{src.Id}|{src.Id}");

        Assert.Equal(before, vm.CurrentProblem.Edges.Count);
    }

    [AvaloniaFact]
    public void AddEdge_Duplicate_Ignored()
    {
        var vm = CreateVm();
        vm.AddAgentCommand.Execute("Source");
        var src = vm.SelectedAgent!;
        vm.AddAgentCommand.Execute("ServiceBlock");
        var svb = vm.SelectedAgent!;

        vm.AddEdgeCommand.Execute($"{src.Id}|{svb.Id}");
        var after1 = vm.CurrentProblem.Edges.Count;

        vm.AddEdgeCommand.Execute($"{src.Id}|{svb.Id}");

        Assert.Equal(after1, vm.CurrentProblem.Edges.Count);
    }

    [AvaloniaFact]
    public void AddEdge_NullOrEmpty_Ignored()
    {
        var vm = CreateVm();
        var before = vm.CurrentProblem.Edges.Count;

        vm.AddEdgeCommand.Execute(null);
        vm.AddEdgeCommand.Execute("");

        Assert.Equal(before, vm.CurrentProblem.Edges.Count);
    }

    [AvaloniaFact]
    public void AddEdge_InvalidFormat_Ignored()
    {
        var vm = CreateVm();
        var before = vm.CurrentProblem.Edges.Count;

        vm.AddEdgeCommand.Execute("justOneId");

        Assert.Equal(before, vm.CurrentProblem.Edges.Count);
    }

    [AvaloniaFact]
    public void DeleteAgent_Removes_Connected_Edges()
    {
        var vm = CreateVm();
        vm.AddAgentCommand.Execute("Source");
        var src = vm.SelectedAgent!;
        vm.AddAgentCommand.Execute("ServiceBlock");
        var svb = vm.SelectedAgent!;
        vm.AddEdgeCommand.Execute($"{src.Id}|{svb.Id}");

        vm.DeleteAgentCommand.Execute(src);

        Assert.DoesNotContain(vm.CurrentProblem.Edges, e => e.From == src.Id || e.To == src.Id);
    }

    // ═══════════════════════════════════════════
    //  SelectEdge / DeleteSelectedEdge tests
    // ═══════════════════════════════════════════

    [AvaloniaFact]
    public void SelectEdge_Sets_SelectedEdge()
    {
        var vm = CreateVm();
        vm.AddAgentCommand.Execute("Source");
        var src = vm.SelectedAgent!;
        vm.AddAgentCommand.Execute("ServiceBlock");
        var svb = vm.SelectedAgent!;
        vm.AddEdgeCommand.Execute($"{src.Id}|{svb.Id}");
        var edge = vm.CurrentProblem.Edges.Last();

        vm.SelectEdgeCommand.Execute(edge);

        Assert.Equal(edge, vm.SelectedEdge);
        Assert.Null(vm.SelectedAgent);
    }

    [AvaloniaFact]
    public void DeleteSelectedEdge_Removes_Edge()
    {
        var vm = CreateVm();
        vm.AddAgentCommand.Execute("Source");
        var src = vm.SelectedAgent!;
        vm.AddAgentCommand.Execute("ServiceBlock");
        var svb = vm.SelectedAgent!;
        vm.AddEdgeCommand.Execute($"{src.Id}|{svb.Id}");
        var edge = vm.CurrentProblem.Edges.Last();
        vm.SelectEdgeCommand.Execute(edge);

        vm.DeleteSelectedEdgeCommand.Execute(null);

        Assert.DoesNotContain(vm.CurrentProblem.Edges, e => e == edge);
        Assert.Null(vm.SelectedEdge);
    }

    [AvaloniaFact]
    public void DeleteSelectedEdge_NothingSelected_NoOp()
    {
        var vm = CreateVm();
        var before = vm.CurrentProblem.Edges.Count;

        vm.DeleteSelectedEdgeCommand.Execute(null);

        Assert.Equal(before, vm.CurrentProblem.Edges.Count);
    }
}
