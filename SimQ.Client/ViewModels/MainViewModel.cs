using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    public ObservableCollection<RunRecord> RunHistory { get; }
    public ObservableCollection<ResultRow> ResultTable { get; }

    [ObservableProperty] private ProblemViewModel _currentProblem;
    [ObservableProperty] private AgentViewModel? _selectedAgent;
    [ObservableProperty] private AppScreen _screen = AppScreen.Editor;
    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private double _accentHue = 250;

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private double _progress = 0.42;
    [ObservableProperty] private int _iterations = 50000;
    [ObservableProperty] private int _seed = 42;
    [ObservableProperty] private string _toast = "";
    [ObservableProperty] private bool _hasToast;
    [ObservableProperty] private bool _tweaksOpen;

    // ---------- API state ----------
    private readonly SimQApiClient? _api;
    private CancellationTokenSource? _healthCts;
    private CancellationTokenSource? _simulationCts;

    [ObservableProperty] private bool _isApiOnline;
    [ObservableProperty] private string _apiStatusText = "SimQ.Core: проверка…";
    [ObservableProperty] private string _apiBaseUrl = "";
    [ObservableProperty] private string? _currentTaskId;

    [ObservableProperty] private string _wizardName = "";
    [ObservableProperty] private string _wizardDescription = "";
    [ObservableProperty] private int _wizardTemplate;
    [ObservableProperty] private int _wizardStep = 1;

    partial void OnWizardStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsWizardStep1));
        OnPropertyChanged(nameof(IsWizardStep2));
        OnPropertyChanged(nameof(IsWizardStep3));
        OnPropertyChanged(nameof(WizardStepLabel));
        OnPropertyChanged(nameof(WizardStepTitle));
        OnPropertyChanged(nameof(WizardStepHint));
        OnPropertyChanged(nameof(CanWizardBack));
        OnPropertyChanged(nameof(CanWizardNext));
        OnPropertyChanged(nameof(WizardStep1Brush));
        OnPropertyChanged(nameof(WizardStep2Brush));
        OnPropertyChanged(nameof(WizardStep3Brush));
        OnPropertyChanged(nameof(WizardStep1Opacity));
        OnPropertyChanged(nameof(WizardStep2Opacity));
        OnPropertyChanged(nameof(WizardStep3Opacity));
    }

    public bool IsWizardStep1 => WizardStep == 1;
    public bool IsWizardStep2 => WizardStep == 2;
    public bool IsWizardStep3 => WizardStep == 3;
    public bool CanWizardBack => WizardStep > 1;
    public bool CanWizardNext => WizardStep < 3;

    public string WizardStepLabel => $"ШАГ {WizardStep} / 3";
    public string WizardStepTitle => WizardStep switch
    {
        1 => "Описание задачи",
        2 => "Структура агентов",
        3 => "Параметры моделирования",
        _ => "",
    };
    public string WizardStepHint => WizardStep switch
    {
        1 => "Имя, описание и шаблон — для последующего поиска в истории.",
        2 => "Шаблон загружает типовую структуру агентов. Её можно править в редакторе.",
        3 => "Параметры запуска можно изменить позже на экране «Моделирование».",
        _ => "",
    };

    private static readonly Avalonia.Media.IBrush AccentBrushStatic =
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3D5FCC"));
    private static readonly Avalonia.Media.IBrush MutedBrushStatic =
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#DFE3EA"));

    public Avalonia.Media.IBrush WizardStep1Brush => WizardStep >= 1 ? AccentBrushStatic : MutedBrushStatic;
    public Avalonia.Media.IBrush WizardStep2Brush => WizardStep >= 2 ? AccentBrushStatic : MutedBrushStatic;
    public Avalonia.Media.IBrush WizardStep3Brush => WizardStep >= 3 ? AccentBrushStatic : MutedBrushStatic;
    public double WizardStep1Opacity => WizardStep == 1 ? 1.0 : 0.55;
    public double WizardStep2Opacity => WizardStep == 2 ? 1.0 : 0.55;
    public double WizardStep3Opacity => WizardStep == 3 ? 1.0 : 0.55;

    public void WizardCancel()
    {
        WizardStep = 1;
        Screen = AppScreen.Editor;
    }
    public void WizardBack()
    {
        if (WizardStep > 1) WizardStep--;
    }
    public void WizardNext()
    {
        if (WizardStep < 3) WizardStep++;
    }
    public Task WizardCreateAsync() => CreateProblemAsync();

    public bool IsEditor   { get => Screen == AppScreen.Editor;   set { if (value) Screen = AppScreen.Editor;   } }
    public bool IsSimulate { get => Screen == AppScreen.Simulate; set { if (value) Screen = AppScreen.Simulate; } }
    public bool IsResults  { get => Screen == AppScreen.Results;  set { if (value) Screen = AppScreen.Results;  } }
    public bool IsTasks    { get => Screen == AppScreen.Tasks;    set { if (value) Screen = AppScreen.Tasks;    } }
    public bool IsWizard   { get => Screen == AppScreen.Wizard;   set { if (value) Screen = AppScreen.Wizard;   } }

    partial void OnScreenChanged(AppScreen value)
    {
        OnPropertyChanged(nameof(IsEditor));
        OnPropertyChanged(nameof(IsSimulate));
        OnPropertyChanged(nameof(IsResults));
        OnPropertyChanged(nameof(IsTasks));
        OnPropertyChanged(nameof(IsWizard));
        OnPropertyChanged(nameof(ScreenLabel));

        if (value == AppScreen.Tasks)
            _ = RefreshHistoryAsync();
    }

    public string ScreenLabel => Screen switch
    {
        AppScreen.Editor   => "01 Редактор модели",
        AppScreen.Simulate => "02 Моделирование",
        AppScreen.Results  => "03 Результаты",
        AppScreen.Tasks    => "04 История",
        AppScreen.Wizard   => "05 Новая задача",
        _ => Screen.ToString()
    };

    public MainViewModel() : this(null) { }

    public MainViewModel(SimQApiClient? api)
    {
        _api = api;
        _apiBaseUrl = api?.Settings.BaseUrl ?? ApiSettings.DefaultBaseUrl;

        var problems = SampleData.CreateProblems();
        Problems = new ObservableCollection<ProblemViewModel>(problems.Select(p => new ProblemViewModel(p)));
        RunHistory = new ObservableCollection<RunRecord>(SampleData.CreateRunHistory());
        ResultTable = new ObservableCollection<ResultRow>(SampleData.CreateResultTable());
        _currentProblem = Problems[0];
        _selectedAgent = _currentProblem.Agents.FirstOrDefault(a => a.Id == "a4");

        if (_api != null)
            StartHealthMonitor();
    }

    // ---------- Health monitor ----------

    private void StartHealthMonitor()
    {
        _healthCts = new CancellationTokenSource();
        _ = HealthLoopAsync(_healthCts.Token);
    }

    private async Task HealthLoopAsync(CancellationToken ct)
    {
        if (_api == null) return;
        var firstSuccess = true;
        while (!ct.IsCancellationRequested)
        {
            var ok = await _api.CheckHealthAsync(ct);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsApiOnline = ok;
                ApiStatusText = ok
                    ? $"SimQ.Core доступен ({_api.Settings.BaseUrl})"
                    : $"SimQ.Core недоступен — {_api.LastError ?? "нет связи"}";
            });

            if (firstSuccess && ok)
            {
                firstSuccess = false;
                await TryLoadProblemsFromApiAsync(ct);
            }

            try { await Task.Delay(_api.Settings.HealthCheckInterval, ct); }
            catch (TaskCanceledException) { return; }
        }
    }

    private async Task TryLoadProblemsFromApiAsync(CancellationToken ct)
    {
        if (_api == null) return;
        var list = await _api.GetProblemsAsync(ct);
        if (list == null) return;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ShowToast($"API: задач на сервере — {list.Total}");
        });
    }

    [RelayCommand]
    private void Navigate(string target)
    {
        if (Enum.TryParse<AppScreen>(target, true, out var s)) Screen = s;
    }

    [RelayCommand]
    private void PickProblem(ProblemViewModel p)
    {
        CurrentProblem = p;
        SelectedAgent = null;
    }

    [RelayCommand]
    private void SelectAgent(AgentViewModel? a) => SelectedAgent = a;

    [RelayCommand]
    private void DeleteAgent(AgentViewModel? a)
    {
        if (a == null) return;
        CurrentProblem.Agents.Remove(a);
        var dead = CurrentProblem.Edges.Where(e => e.From == a.Id || e.To == a.Id).ToList();
        foreach (var e in dead) CurrentProblem.Edges.Remove(e);
        if (SelectedAgent == a) SelectedAgent = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_api == null || !IsApiOnline)
        {
            ShowToast("API недоступен — задача не отправлена");
            return;
        }

        var request = ProblemMapper.ToRegisterRequest(CurrentProblem.Model);
        var resp = await _api.RegisterProblemAsync(request);
        if (resp == null)
        {
            ShowToast($"Ошибка сохранения: {_api.LastError ?? "нет ответа"}");
            return;
        }

        if (!string.IsNullOrEmpty(resp.Id))
            CurrentProblem.Model.Id = resp.Id!;

        ShowToast($"Сохранено — id {resp.Id}");
    }

    [RelayCommand]
    private async Task StartSimulationAsync()
    {
        if (IsRunning) return;

        if (_api == null || !IsApiOnline)
        {
            ShowToast("API недоступен — запуск отменён");
            return;
        }

        if (string.IsNullOrEmpty(CurrentProblem.Model.Id) ||
            CurrentProblem.Model.Id.StartsWith("p-", StringComparison.Ordinal))
        {
            ShowToast("Сначала сохраните задачу (Сохранить)");
            return;
        }

        IsRunning = true;
        Progress = 0;
        CurrentTaskId = null;
        _simulationCts = new CancellationTokenSource();
        var ct = _simulationCts.Token;

        try
        {
            var created = await _api.CreateTaskAsync(new CreateTaskRequestDto
            {
                ProblemId = CurrentProblem.Model.Id,
                MaxSteps = (uint)Math.Max(1, Iterations),
            }, ct);

            if (created == null || string.IsNullOrEmpty(created.TaskId))
            {
                ShowToast($"Не удалось создать задачу: {_api.LastError ?? "нет ответа"}");
                IsRunning = false;
                return;
            }

            CurrentTaskId = created.TaskId;
            ShowToast($"Задача поставлена: {created.TaskId}");

            while (!ct.IsCancellationRequested)
            {
                var task = await _api.GetTaskAsync(created.TaskId, ct);
                if (task == null)
                {
                    ShowToast($"Не удалось получить статус: {_api.LastError ?? "нет ответа"}");
                    break;
                }

                Progress = MapProgress(task);
                if (IsTerminal(task.Status))
                {
                    Progress = 1;
                    ShowToast($"Завершено: {task.Status}");
                    break;
                }

                try { await Task.Delay(750, ct); } catch (TaskCanceledException) { break; }
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Ошибка симуляции: {ex.Message}");
        }
        finally
        {
            IsRunning = false;
        }
    }

    private static bool IsTerminal(string status)
        => status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
        || status.Equals("Error",     StringComparison.OrdinalIgnoreCase)
        || status.Equals("Canceled",  StringComparison.OrdinalIgnoreCase);

    private static double MapProgress(SimulationTaskDto t)
    {
        if (t.ResultData == null)
            return t.Status.Equals("Modelling", StringComparison.OrdinalIgnoreCase) ? 0.05 : 0;
        var byEvents = t.ResultData.MaxEventsAmount > 0
            ? t.ResultData.CurrentEventsAmount / t.ResultData.MaxEventsAmount : 0;
        var byTime = t.ResultData.MaxModelationTime > 0
            ? t.ResultData.CurrentModelationTime / t.ResultData.MaxModelationTime : 0;
        return Math.Clamp(Math.Max(byEvents, byTime), 0, 1);
    }

    [RelayCommand]
    private async Task StopSimulationAsync()
    {
        _simulationCts?.Cancel();
        if (_api != null && !string.IsNullOrEmpty(CurrentTaskId))
        {
            var ok = await _api.StopTaskAsync(CurrentTaskId);
            ShowToast(ok ? "Симуляция остановлена" : $"Ошибка остановки: {_api.LastError}");
        }
        IsRunning = false;
    }

    [RelayCommand] private void OpenResults() => Screen = AppScreen.Results;
    [RelayCommand] private void OpenWizard() => Screen = AppScreen.Wizard;
    [RelayCommand] private void CancelWizard() => Screen = AppScreen.Editor;

    [RelayCommand]
    public async Task CreateProblemAsync()
    {
        var name = string.IsNullOrWhiteSpace(WizardName) ? "New problem" : WizardName.Trim();

        var problem = new Problem
        {
            Id = string.Empty,
            Name = name,
            Description = WizardDescription ?? string.Empty,
            CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            ModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            Status = ProblemStatus.Draft,
            Agents =
            {
                new Agent { Id = "src1", Kind = AgentKind.Source,       Name = "Source #1",       X =  60, Y = 200,
                            ArrivalDistribution = new() { Kind = DistributionKind.M, Rate = 0.3 } },
                new Agent { Id = "svb1", Kind = AgentKind.ServiceBlock, Name = "ServiceBlock #1", X = 360, Y = 200,
                            Channels = 1,
                            ServiceDistribution = new() { Kind = DistributionKind.M, Rate = 0.5 } },
                new Agent { Id = "snk1", Kind = AgentKind.Sink,         Name = "Sink",            X = 660, Y = 200 },
            },
            Edges =
            {
                new Edge { Id = "e1", From = "src1", To = "svb1" },
                new Edge { Id = "e2", From = "svb1", To = "snk1" },
            },
        };

        var vm = new ProblemViewModel(problem);

        // Always create locally so the user lands in the editor with the new problem.
        problem.Id = $"local-{Guid.NewGuid():N}".Substring(0, 12);
        Problems.Add(vm);
        CurrentProblem = vm;
        SelectedAgent = null;
        WizardName = string.Empty;
        WizardDescription = string.Empty;
        WizardStep = 1;
        Screen = AppScreen.Editor;
        ShowToast($"Задача создана: {problem.Id}");

        // Best-effort sync to the API; failure doesn't block the UI flow.
        if (_api != null && IsApiOnline)
        {
            var resp = await _api.RegisterProblemAsync(ProblemMapper.ToRegisterRequest(problem));
            if (resp != null && !string.IsNullOrEmpty(resp.Id))
            {
                problem.Id = resp.Id!;
                ShowToast($"Сохранено на сервере: {resp.Id}");
            }
            else
            {
                ShowToast($"API: {_api.LastError ?? "нет ответа"}");
            }
        }
    }

    private async Task RefreshHistoryAsync()
    {
        if (_api == null || !IsApiOnline) return;
        var list = await _api.GetTasksAsync();
        if (list == null) return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            RunHistory.Clear();
            foreach (var t in list.Tasks)
            {
                RunHistory.Add(new RunRecord
                {
                    Id = t.TaskId,
                    ProblemId = "",
                    ProblemName = t.TaskId,
                    Status = t.Status switch
                    {
                        "Completed" => RunStatus.Done,
                        "Error"     => RunStatus.Failed,
                        "Canceled"  => RunStatus.Cancelled,
                        _           => RunStatus.Running,
                    },
                    StartedAt = t.Started?.ToString("yyyy-MM-dd HH:mm") ?? "",
                    Duration = (t.Started.HasValue && t.Finished.HasValue)
                        ? (t.Finished.Value - t.Started.Value).ToString(@"hh\:mm\:ss") : "",
                    Iterations = (int)(t.ResultData?.CurrentEventsAmount ?? 0),
                });
            }
        });
    }

    [RelayCommand] private void ToggleTweaks() => TweaksOpen = !TweaksOpen;

    [RelayCommand]
    private void SetAccent(string hue)
    {
        if (double.TryParse(hue, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var h)) AccentHue = h;
    }

    public void ShowToast(string msg)
    {
        Toast = msg;
        HasToast = true;
        DispatcherTimer.RunOnce(() => HasToast = false, TimeSpan.FromSeconds(2.4));
    }

    public double MaxProbability => ResultTable.Count == 0 ? 1 : ResultTable.Max(r => r.P);
    public string IterationsLabel => $"{(int)(Progress * Iterations):N0} / {Iterations:N0} итераций";
    public string ProgressPercent => $"{Progress * 100:F1}";
}
