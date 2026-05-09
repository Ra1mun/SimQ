using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimQ.Client.Models;
using SimQ.Client.Services;

namespace SimQ.Client.ViewModels;

public enum AppScreen { Editor, Simulate, Results, Tasks, Wizard }

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<ProblemViewModel> Problems { get; }
    public ObservableCollection<ProblemViewModel> FilteredProblems { get; } = new();
    public ObservableCollection<RunRecord> RunHistory { get; }
    public ObservableCollection<ResultRow> ResultTable { get; }
    public ObservableCollection<SimLogEntry> SimulationLogs { get; } = new();

    [ObservableProperty] private ProblemViewModel _currentProblem;
    [ObservableProperty] private AgentViewModel? _selectedAgent;
    [ObservableProperty] private Edge? _selectedEdge;
    [ObservableProperty] private AppScreen _screen = AppScreen.Editor;
    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private double _accentHue = 250;

    [ObservableProperty] private string _toast = "";
    [ObservableProperty] private bool _hasToast;
    [ObservableProperty] private bool _toastIsError;
    [ObservableProperty] private bool _tweaksOpen;

    [ObservableProperty] private string _problemSearchQuery = "";

    [ObservableProperty] private bool _isEditor   = true;
    [ObservableProperty] private bool _isSimulate;
    [ObservableProperty] private bool _isResults;
    [ObservableProperty] private bool _isTasks;
    [ObservableProperty] private bool _isWizard;

    public string ScreenLabel => Screen switch
    {
        AppScreen.Editor   => "01 Редактор модели",
        AppScreen.Simulate => "02 Моделирование",
        AppScreen.Results  => "03 Результаты",
        AppScreen.Tasks    => "04 История",
        AppScreen.Wizard   => "05 Новая задача",
        _ => Screen.ToString(),
    };

    partial void OnScreenChanged(AppScreen value)
    {
        IsEditor   = value == AppScreen.Editor;
        IsSimulate = value == AppScreen.Simulate;
        IsResults  = value == AppScreen.Results;
        IsTasks    = value == AppScreen.Tasks;
        IsWizard   = value == AppScreen.Wizard;
        OnPropertyChanged(nameof(ScreenLabel));

        if (value == AppScreen.Tasks)   _ = RefreshHistoryAsync();
        if (value == AppScreen.Results) _ = RefreshResultsAsync();
    }

    partial void OnCurrentProblemChanged(ProblemViewModel value)
    {
        SelectedAgent = value.Agents.FirstOrDefault();
        OnPropertyChanged(nameof(EndpointTaskIdLabel));
        OnPropertyChanged(nameof(ResultsSubtitle));
    }

    partial void OnProblemSearchQueryChanged(string value)
    {
        FilteredProblems.Clear();
        var q = value.Trim();
        foreach (var p in Problems)
        {
            if (string.IsNullOrEmpty(q) ||
                p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(q, StringComparison.OrdinalIgnoreCase))
                FilteredProblems.Add(p);
        }
    }

    public MainViewModel() : this(null) { }

    public MainViewModel(SimQApiClient? api)
    {
        _api = api;
        _apiBaseUrl = api?.Settings.BaseUrl ?? ApiSettings.DefaultBaseUrl;

        Problems    = new ObservableCollection<ProblemViewModel>();
        RunHistory  = new ObservableCollection<RunRecord>();
        ResultTable = new ObservableCollection<ResultRow>();

        InitWizardState();

        var placeholder = CreateEmptyProblem();
        var placeholderVm = new ProblemViewModel(placeholder);
        Problems.Add(placeholderVm);
        FilteredProblems.Add(placeholderVm);
        _currentProblem = placeholderVm;
        _selectedAgent  = null;

        SeedSampleResults();

        if (_api != null)
            StartHealthMonitor();
    }

    public void ShowToast(string msg)
    {
        Toast = msg;
        ToastIsError = false;
        HasToast = true;
        DispatcherTimer.RunOnce(() => HasToast = false, TimeSpan.FromSeconds(2.4));
    }

    public void ShowError(string msg)
    {
        Toast = msg;
        ToastIsError = true;
        HasToast = true;
        DispatcherTimer.RunOnce(() => HasToast = false, TimeSpan.FromSeconds(3.5));
    }

    [RelayCommand] private void ToggleTweaks() => TweaksOpen = !TweaksOpen;

    [RelayCommand]
    private void SetAccent(string hue)
    {
        if (double.TryParse(hue, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var h))
            AccentHue = h;
    }

    private static Problem CreateEmptyProblem() => new()
    {
        Id          = "",
        Name        = "—",
        Description = "Нет загруженных задач",
        CreatedAt   = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
        ModifiedAt  = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
        Status      = ProblemStatus.Draft,
    };
}
