using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using SimQ.Client.ViewModels;
using SimQ.Client.Views;

namespace SimQ.Client.Tests;

/// <summary>
/// Headless UI tests for MainWindow: rendering, navigation, and basic interactions.
/// </summary>
public class MainWindowTests
{
    private static (MainWindow window, MainViewModel vm) CreateWindow()
    {
        var vm = new MainViewModel(null); // no API — offline mode
        var window = new MainWindow { DataContext = vm };
        window.Show();
        return (window, vm);
    }

    [AvaloniaFact]
    public void MainWindow_Should_Render_Without_Errors()
    {
        var (window, _) = CreateWindow();
        Assert.NotNull(window);
        Assert.True(window.IsVisible);
    }

    [AvaloniaFact]
    public void MainWindow_Title_Should_Contain_SimQ()
    {
        var (window, _) = CreateWindow();
        Assert.Contains("SimQ", window.Title);
    }

    [AvaloniaFact]
    public void Default_Screen_Should_Be_Editor()
    {
        var (_, vm) = CreateWindow();
        Assert.Equal(AppScreen.Editor, vm.Screen);
        Assert.True(vm.IsEditor);
        Assert.False(vm.IsSimulate);
        Assert.False(vm.IsResults);
    }

    [AvaloniaFact]
    public void Navigate_To_Simulate_Screen()
    {
        var (_, vm) = CreateWindow();

        vm.SelectScreenCommand.Execute("Simulate");

        Assert.Equal(AppScreen.Simulate, vm.Screen);
        Assert.True(vm.IsSimulate);
        Assert.False(vm.IsEditor);
    }

    [AvaloniaFact]
    public void Navigate_To_Results_Screen()
    {
        var (_, vm) = CreateWindow();

        vm.SelectScreenCommand.Execute("Results");

        Assert.Equal(AppScreen.Results, vm.Screen);
        Assert.True(vm.IsResults);
    }

    [AvaloniaFact]
    public void Navigate_To_Tasks_Screen()
    {
        var (_, vm) = CreateWindow();

        vm.SelectScreenCommand.Execute("Tasks");

        Assert.Equal(AppScreen.Tasks, vm.Screen);
        Assert.True(vm.IsTasks);
    }

    [AvaloniaFact]
    public void CurrentProblem_Should_Not_Be_Null()
    {
        var (_, vm) = CreateWindow();
        Assert.NotNull(vm.CurrentProblem);
        Assert.False(string.IsNullOrEmpty(vm.CurrentProblem.Name));
    }

    [AvaloniaFact]
    public void Problems_Collection_Should_Not_Be_Empty()
    {
        var (_, vm) = CreateWindow();
        Assert.NotEmpty(vm.Problems);
    }

    [AvaloniaFact]
    public void IsRunning_Should_Default_To_False()
    {
        var (_, vm) = CreateWindow();
        Assert.False(vm.IsRunning);
    }

    [AvaloniaFact]
    public void ScreenLabel_Should_Reflect_Current_Screen()
    {
        var (_, vm) = CreateWindow();

        Assert.Contains("Редактор", vm.ScreenLabel);

        vm.SelectScreenCommand.Execute("Simulate");
        Assert.Contains("Моделирование", vm.ScreenLabel);
    }
}
