using Avalonia.Controls;
using Avalonia.Input;
using SimQ.Client.ViewModels;

namespace SimQ.Client.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnMinimize(object? s, Avalonia.Interactivity.RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void OnMaximize(object? s, Avalonia.Interactivity.RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object? s, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private void OnTabClick(object? s, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (s is not Button btn) return;
        var tag = btn.Tag as string;
        vm.Screen = tag switch
        {
            "Editor"   => AppScreen.Editor,
            "Simulate" => AppScreen.Simulate,
            "Results"  => AppScreen.Results,
            "Tasks"    => AppScreen.Tasks,
            "Wizard"   => AppScreen.Wizard,
            _          => vm.Screen
        };
    }
}
