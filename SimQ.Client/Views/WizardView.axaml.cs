using Avalonia.Controls;
using Avalonia.Interactivity;
using SimQ.Client.ViewModels;

namespace SimQ.Client.Views;

public partial class WizardView : UserControl
{
    public WizardView() => InitializeComponent();

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.WizardCancelCommand.CanExecute(null))
            vm.WizardCancelCommand.Execute(null);
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.WizardBackCommand.CanExecute(null))
            vm.WizardBackCommand.Execute(null);
    }

    private void OnNextClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.WizardNextCommand.CanExecute(null))
            vm.WizardNextCommand.Execute(null);
    }

    private void OnCreateClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) _ = vm.WizardCreateAsync();
    }
}
