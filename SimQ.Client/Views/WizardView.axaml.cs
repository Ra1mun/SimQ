using Avalonia.Controls;
using Avalonia.Interactivity;
using SimQ.Client.ViewModels;

namespace SimQ.Client.Views;

public partial class WizardView : UserControl
{
    public WizardView() => InitializeComponent();

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.WizardCancel();
    }

    private void OnBackClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.WizardBack();
    }

    private void OnNextClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.WizardNext();
    }

    private void OnCreateClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) _ = vm.WizardCreateAsync();
    }
}
