using Avalonia.Controls;
using Avalonia.Input;
using SimQ.Client.ViewModels;

namespace SimQ.Client.Views;

public partial class EditorView : UserControl
{
    public EditorView() => InitializeComponent();

    private void OnNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.DataContext is AgentViewModel a
            && DataContext is MainViewModel vm)
        {
            vm.SelectAgentCommand.Execute(a);
            e.Handled = true;
        }
    }
}
