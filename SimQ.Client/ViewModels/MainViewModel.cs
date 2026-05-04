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

    // Real backing flags so compiled-binding IsVisible bindings track changes
    // reliably (computed getters were not refreshing on screen/step change).
    [ObservableProperty] private bool _isEditor   = true;
    [ObservableProperty] private bool _isSimulate;
    [ObservableProperty] private bool _isResults;
    [ObservableProperty] private bool _isTasks;
    [ObservableProperty] private bool _isWizard;

    [ObservableProperty] private bool _isWizardStep1 = true;
    [ObservableProperty] private bool _isWizardStep2;
    [ObservableProperty] private bool _isWizardStep3;
    [ObservableProperty] private bool _canWizardBack;
    [ObservableProperty] private bool _canWizardNext = true;

    [ObservableProperty] private string _wizardStepLabel = "ШАГ 1 / 3";
    [ObservableProperty] private string _wizardStepTitle = "Описание задачи";
    [ObservableProperty] private string _wizardStepHint =
        "Имя, описание и шаблон — для последующего поиска в истории.";

    private static readonly Avalonia.Media.IBrush AccentBrushStatic =
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#3D5FCC"));
    private static readonly Avalonia.Media.IBrush MutedBrushStatic =
        new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#DFE3EA"));

    [ObservableProperty] private Avalonia.Media.IBrush _wizardStep1Brush = AccentBrushStatic;
    [ObservableProperty] private Avalonia.Media.IBrush _wizardStep2Brush = MutedBrushStatic;
    [ObservableProperty] private Avalonia.Media.IBrush _wizardStep3Brush = MutedBrushStatic;
    [ObservableProperty] private double _wizardStep1Opacity = 1.0;
    [ObservableProperty] private double _wizardStep2Opacity = 0.55;
    [ObservableProperty] private double _wizardStep3Opacity = 0.55;

    partial void OnWizardStepChanged(int value)
    {
        var step = Math.Clamp(value, 1, 3);
        IsWizardStep1 = step == 1;
        IsWizardStep2 = step == 2;
        IsWizardStep3 = step == 3;
        CanWizardBack = step > 1;
        CanWizardNext = step < 3;

        WizardStepLabel = $"ШАГ {step} / 3";
        WizardStepTitle = step switch
        {
            1 => "Описание задачи",
            2 => "Структура агентов",
            3 => "Параметры моделирования",
            _ => "",
        };
        WizardStepHint = step switch
        {
            1 => "Имя, описание и шаблон — для последующего поиска в истории.",
            2 => "Шаблон загружает типовую структуру агентов. Её можно править в редакторе.",
            3 => "Параметры запуска можно изменить позже на экране «Моделирование».",
            _ => "",
        };

        WizardStep1Brush   = step >= 1 ? AccentBrushStatic : MutedBrushStatic;
        WizardStep2Brush   = step >= 2 ? AccentBrushStatic : MutedBrushStatic;
        WizardStep3Brush   = step >= 3 ? AccentBrushStatic : MutedBrushStatic;
        WizardStep1Opacity = step == 1 ? 1.0 : 0.55;
        WizardStep2Opacity = step == 2 ? 1.0 : 0.55;
        WizardStep3Opacity = step == 3 ? 1.0 : 0.55;
    }

    [RelayCommand] private void WizardCancel()
    {
        WizardStep = 1;
        Screen = AppScreen.Editor;
    }
    [RelayCommand(CanExecute = nameof(CanWizardBack))]
    private void WizardBack()
    {
        if (WizardStep > 1) WizardStep--;
    }
    [RelayCommand(CanExecute = nameof(CanWizardNext))]
    private void WizardNext()
    {
        if (WizardStep < 3) WizardStep++;
    }
    public Task WizardCreateAsync() => CreateProblemAsync();

    partial void OnCanWizardBackChanged(bool value) => WizardBackCommand.NotifyCanExecuteChanged();
    partial void OnCanWizardNextChanged(bool value) => WizardNextCommand.NotifyCanExecuteChanged();

    partial void OnScreenChanged(AppScreen value)
    {
        IsEditor   = value == AppScreen.Editor;
        IsSimulate = value == AppScreen.Simulate;
        IsResults  = value == AppScreen.Results;
        IsTasks    = value == AppScreen.Tasks;
        IsWizard   = value == AppScreen.Wizard;
        OnPropertyChanged(nameof(ScreenLabel));

        if (value == AppScreen.Tasks)
            _ = RefreshHistoryAsync();
        if (value == AppScreen.Results)
            _ = RefreshResultsAsync();
    }

    partial void OnCurrentProblemChanged(ProblemViewModel value)
    {
        SelectedAgent = value?.Agents.FirstOrDefault();
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
            bool ok = false;
            try
            {
                ok = await _api.CheckHealthAsync(ct);
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsApiOnline = false;
                    ApiStatusText = $"SimQ.Core недоступен — {ex.Message}";
                });
            }

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
                try { await TryLoadProblemsFromApiAsync(ct); }
                catch (OperationCanceledException) { return; }
                catch (Exception ex)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => ShowToast($"API: {ex.Message}"));
                }
            }

            try { await Task.Delay(_api.Settings.HealthCheckInterval, ct); }
            catch (TaskCanceledException) { return; }
            catch (OperationCanceledException) { return; }
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
    private void SelectScreen(string? target)
    {
        if (string.IsNullOrEmpty(target)) return;
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
    private void AddAgent(string? kindStr)
    {
        if (string.IsNullOrEmpty(kindStr)) return;
        if (!Enum.TryParse<AgentKind>(kindStr, true, out var kind)) return;

        var id = $"{kind.ToString().ToLower()[..3]}{CurrentProblem.Agents.Count + 1}";
        var agent = new Agent
        {
            Id = id,
            Kind = kind,
            Name = $"{kind} #{CurrentProblem.Agents.Count(a => a.Kind == kind) + 1}",
            X = 100 + CurrentProblem.Agents.Count * 60,
            Y = 200,
        };
        CurrentProblem.Model.Agents.Add(agent);
        var vm = new AgentViewModel(agent);
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(AgentViewModel.X) or nameof(AgentViewModel.Y))
                CurrentProblem.Model.RebuildEdgeAnchors();
        };
        CurrentProblem.Agents.Add(vm);
        SelectedAgent = vm;
        ShowToast($"Добавлен: {agent.Name}");
    }

    [RelayCommand]
    private void DeleteAgent(AgentViewModel? a)
    {
        if (a == null) return;
        CurrentProblem.Agents.Remove(a);
        CurrentProblem.Model.Agents.RemoveAll(x => x.Id == a.Id);
        var dead = CurrentProblem.Edges.Where(e => e.From == a.Id || e.To == a.Id).ToList();
        foreach (var e in dead) CurrentProblem.Edges.Remove(e);
        CurrentProblem.Model.Edges.RemoveAll(e => e.From == a.Id || e.To == a.Id);
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

    [RelayCommand]
    public async Task CreateProblemAsync()
    {
        try
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
                try
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
                catch (Exception ex)
                {
                    ShowToast($"API ошибка: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Не удалось создать задачу: {ex.Message}");
        }
    }

    private async Task RefreshHistoryAsync()
    {
        if (_api == null || !IsApiOnline) return;
        try
        {
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
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShowToast($"История недоступна: {ex.Message}"));
        }
    }

    private async Task RefreshResultsAsync()
    {
        if (_api == null || !IsApiOnline)
        {
            ShowToast("API недоступен — результаты не загружены");
            return;
        }

        try
        {
            var problemId = CurrentProblem?.Model?.Id;
            var resp = await _api.GetResultsAsync(problemId, CurrentTaskId);
            if (resp == null)
            {
                ShowToast($"Результаты: {_api.LastError ?? "нет данных"}");
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ResultTable.Clear();

                // Aggregate state probabilities from all results into ResultRow entries
                var probabilities = new Dictionary<int, double>();
                foreach (var r in resp.Results)
                {
                    if (r.Data?.AgentResults == null) continue;
                    foreach (var agent in r.Data.AgentResults)
                    {
                        foreach (var kv in agent.StatesProbabilities)
                        {
                            if (int.TryParse(kv.Key, out var n))
                                probabilities[n] = probabilities.GetValueOrDefault(n) + kv.Value;
                        }
                    }
                }

                if (probabilities.Count == 0) return;

                var total = probabilities.Values.Sum();
                double cdf = 0;
                double maxP = probabilities.Values.Max() / total;

                foreach (var kv in probabilities.OrderBy(p => p.Key))
                {
                    var p = kv.Value / total;
                    cdf += p;
                    ResultTable.Add(new ResultRow
                    {
                        N = kv.Key,
                        P = p,
                        Cdf = cdf,
                        BarWidth = maxP > 0 ? p / maxP * 220 : 0,
                        ChartHeight = maxP > 0 ? p / maxP * 200 : 0,
                    });
                }

                OnPropertyChanged(nameof(MaxProbability));
                ShowToast($"Загружено результатов: {resp.Results.Length}");
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShowToast($"Ошибка загрузки результатов: {ex.Message}"));
        }
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
