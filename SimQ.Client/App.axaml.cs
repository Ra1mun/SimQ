using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SimQ.Client.Services;
using SimQ.Client.ViewModels;
using SimQ.Client.Views;

namespace SimQ.Client;

public partial class App : Application
{
    private SimQApiClient? _apiClient;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _apiClient = new SimQApiClient(ApiSettings.FromEnvironment());
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(_apiClient)
            };
            desktop.Exit += (_, _) => _apiClient?.Dispose();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
