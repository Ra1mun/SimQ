using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
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
    public bool HasResults => ResultTable.Count > 0;

    [ObservableProperty] private ProblemViewModel _currentProblem;
    [ObservableProperty] private AgentViewModel? _selectedAgent;
    [ObservableProperty] private Edge? _selectedEdge;
    [ObservableProperty] private AppScreen _screen = AppScreen.Editor;
    [ObservableProperty] private double _zoom = 1.0;
    [ObservableProperty] private double _accentHue = 250;

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private int _iterations = 50000;
    [ObservableProperty] private int _seed = 42;
    [ObservableProperty] private string _toast = "";
    [ObservableProperty] private bool _hasToast;
    [ObservableProperty] private bool _tweaksOpen;

    // ---- Simulate screen extras ----
    [ObservableProperty] private int _modelTime = 10000;

    [ObservableProperty] private bool _collectStepStats = true;
    [ObservableProperty] private bool _saveRequestLogs;
    [ObservableProperty] private bool _exportCsvOnFinish = true;

    [ObservableProperty] private string _runId = "";
    [ObservableProperty] private string _elapsedLabel  = "00:00:00";
    [ObservableProperty] private string _remainingLabel = "—";
    [ObservableProperty] private string _speedLabel    = "—";
    [ObservableProperty] private string _cpuLoadLabel  = "—";

    [ObservableProperty] private string _arrivedLabel = "—";
    [ObservableProperty] private string _servedLabel  = "—";
    [ObservableProperty] private string _queueLabel   = "—";
    [ObservableProperty] private string _avgTimeLabel = "—";

    public ObservableCollection<SimLogEntry> SimulationLogs { get; } = new();

    public string EndpointTaskIdLabel
        => string.IsNullOrEmpty(CurrentTaskId)
            ? (CurrentProblem.Model.Id)
            : CurrentTaskId;

    partial void OnCurrentTaskIdChanged(string? value)
    {
        OnPropertyChanged(nameof(EndpointTaskIdLabel));
        OnPropertyChanged(nameof(ResultsSubtitle));
    }

    partial void OnProgressChanged(double value)
    {
        OnPropertyChanged(nameof(IterationsLabel));
        OnPropertyChanged(nameof(ProgressPercent));
    }

    partial void OnIterationsChanged(int value)
    {
        OnPropertyChanged(nameof(IterationsLabel));
    }

    [ObservableProperty] private string _problemSearchQuery = "";
    public ObservableCollection<ProblemViewModel> FilteredProblems { get; } = new();

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

    // ---------- API state ----------
    private readonly SimQApiClient? _api;
    private CancellationTokenSource? _healthCts;
    private CancellationTokenSource? _simulationCts;

    [ObservableProperty] private bool _isApiOnline;
    [ObservableProperty] private string _apiStatusText = "SimQ.Core: проверка…";
    [ObservableProperty] private string _apiBaseUrl;
    [ObservableProperty] private string? _currentTaskId;

    [ObservableProperty] private string _wizardName = "";
    [ObservableProperty] private string _wizardDescription = "";
    [ObservableProperty] private int _wizardTemplate;
    [ObservableProperty] private int _wizardStep = 1;

    [ObservableProperty] private string _wizardTemplateDescription =
        "Задача будет создана без агентов. Добавить агентов и связи можно в редакторе после создания задачи.";
    [ObservableProperty] private string _wizardTemplateAgents = "—";

    partial void OnWizardTemplateChanged(int value)
    {
        (WizardTemplateDescription, WizardTemplateAgents) = value switch
        {
            0 => (
                "Задача будет создана без агентов. Добавить агентов и связи можно в редакторе после создания задачи.",
                "—"
            ),
            1 => (
                "Будет создана типовая структура M/M/1: Source → ServiceBlock → Sink.",
                "• Source #1 — экспоненциальный поток λ=0.3\n• ServiceBlock #1 — 1 канал, μ=0.5\n• Sink — выход"
            ),
            2 => (
                "Будет создана структура M/M/c с очередью: Source → Buffer → ServiceBlock → Sink.",
                "• Source #1 — экспоненциальный поток λ=0.5\n• Queue #1 — буфер, ёмкость 50\n• ServiceBlock #1 — 3 канала, μ=0.4\n• Sink — выход"
            ),
            3 => (
                "Будет создана структура с орбитой повторов: заявки, не попавшие в обслуживание, повторяют попытку.",
                "• Source #1 — экспоненциальный поток λ=0.3\n• ServiceBlock #1 — 1 канал, μ=0.5\n• Orbit #1 — повторный поток λ=1.0\n• Sink — выход"
            ),
            _ => (WizardTemplateDescription, WizardTemplateAgents)
        };
    }

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
            _ => ""
        };

        WizardStep1Brush   = step == 1 ? AccentBrushStatic : MutedBrushStatic;
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

    partial void OnCanWizardBackChanged(bool _) => WizardBackCommand.NotifyCanExecuteChanged();
    partial void OnCanWizardNextChanged(bool _) => WizardNextCommand.NotifyCanExecuteChanged();

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
        SelectedAgent = value.Agents.FirstOrDefault();
        OnPropertyChanged(nameof(EndpointTaskIdLabel));
        OnPropertyChanged(nameof(ResultsSubtitle));
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

        Problems = new ObservableCollection<ProblemViewModel>();
        RunHistory = new ObservableCollection<RunRecord>();
        ResultTable = new ObservableCollection<ResultRow>();

        // Start with an empty placeholder problem so editor bindings have a
        // valid context. Real problems come from the API or the wizard.
        var placeholder = CreateEmptyProblem();
        var placeholderVm = new ProblemViewModel(placeholder);
        Problems.Add(placeholderVm);
        FilteredProblems.Add(placeholderVm);
        _currentProblem = placeholderVm;
        _selectedAgent = null;

        // Pre-populate the result table with a representative distribution so
        // the «Результаты» screen has meaningful content even before the user
        // runs a simulation (matches the design mock-up).
        SeedSampleResults();

        if (_api != null)
            StartHealthMonitor();
    }

    private static Problem CreateEmptyProblem() => new()
    {
        Id = "",
        Name = "—",
        Description = "Нет загруженных задач",
        CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
        ModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
        Status = ProblemStatus.Draft,
    };

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
            // Replace placeholder/previous content with real server data.
            Problems.Clear();
            FilteredProblems.Clear();

            foreach (var dto in list.Data)
            {
                var problem = ProblemFromServer.FromDto(dto);
                var vm = new ProblemViewModel(problem);
                Problems.Add(vm);
                FilteredProblems.Add(vm);
            }

            if (Problems.Count > 0)
            {
                CurrentProblem = Problems[0];
                SelectedAgent = CurrentProblem.Agents.FirstOrDefault();
            }
            else
            {
                var empty = new ProblemViewModel(CreateEmptyProblem());
                Problems.Add(empty);
                FilteredProblems.Add(empty);
                CurrentProblem = empty;
                SelectedAgent = null;
            }

            ShowToast($"API: задач на сервере — {list.Total}");
        });

        // Pull task history alongside the problem list so the Tasks screen
        // is populated even before the user navigates to it.
        await RefreshHistoryAsync();
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
    private void SelectAgent(AgentViewModel? a) { SelectedAgent = a; SelectedEdge = null; }

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
    private void AddEdge(string? param)
    {
        // param = "fromId|toId"
        if (string.IsNullOrEmpty(param)) return;
        var parts = param.Split('|');
        if (parts.Length != 2) return;
        var fromId = parts[0];
        var toId = parts[1];
        if (fromId == toId) return;

        // Prevent duplicate edges
        if (CurrentProblem.Edges.Any(e => e.From == fromId && e.To == toId)) return;

        var edge = new Edge
        {
            Id = $"e{CurrentProblem.Edges.Count + 1}_{fromId}_{toId}",
            From = fromId,
            To = toId
        };
        CurrentProblem.Model.Edges.Add(edge);
        CurrentProblem.Edges.Add(edge);
        CurrentProblem.Model.RebuildEdgeAnchors();
        ShowToast($"Связь: {fromId} → {toId}");
    }

    [RelayCommand]
    private void SelectEdge(Edge? e)
    {
        SelectedEdge = e;
        SelectedAgent = null;
    }

    [RelayCommand]
    private void DeleteSelectedEdge()
    {
        if (SelectedEdge == null) return;
        CurrentProblem.Edges.Remove(SelectedEdge);
        CurrentProblem.Model.Edges.Remove(SelectedEdge);
        SelectedEdge = null;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (_api == null || !IsApiOnline)
            {
                ShowToast("API недоступен — задача не отправлена");
                return;
            }

            var request = ProblemMapper.ToRegisterRequest(CurrentProblem.Model);
            var existingId = CurrentProblem.Model.Id;
            var isUpdate = !string.IsNullOrEmpty(existingId) && !existingId.StartsWith("p-", System.StringComparison.Ordinal);

            System.Diagnostics.Debug.WriteLine($"[SAVE] isUpdate={isUpdate}, existingId={existingId}, agents={request.Agents.Count}, links={request.Links.Count}");

            RegisterProblemResponse? resp;
            if (isUpdate)
                resp = await _api.UpdateProblemAsync(existingId!, request);
            else
                resp = await _api.RegisterProblemAsync(request);

            if (resp == null)
            {
                ShowToast($"Ошибка сохранения: {_api.LastError ?? "нет ответа"}");
                return;
            }

            if (!string.IsNullOrEmpty(resp.Id))
                CurrentProblem.Model.Id = resp.Id!;

            ShowToast(isUpdate ? $"Обновлено — id {resp.Id}" : $"Сохранено — id {resp.Id}");
        }
        catch (System.Exception ex)
        {
            ShowToast($"Ошибка: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SAVE] Exception: {ex}");
        }
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
        ResetSimulationStats();
        _simulationCts = new CancellationTokenSource();
        var ct = _simulationCts.Token;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var cpuTimer = new System.Diagnostics.Stopwatch();
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var lastCpuTime = process.TotalProcessorTime;
        cpuTimer.Start();

        AddLog(SimLogLevel.Info, "client", $"Запуск моделирования (iter={Iterations:N0})");

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
                AddLog(SimLogLevel.Error, "api", $"CreateTask: {_api.LastError ?? "нет ответа"}");
                IsRunning = false;
                return;
            }

            CurrentTaskId = created.TaskId;
            RunId = ShortRunId(created.TaskId);
            ShowToast($"Задача поставлена: {created.TaskId}");
            AddLog(SimLogLevel.Ok, "api", $"Задача создана: {created.TaskId}");

            string? lastStatus = null;
            while (!ct.IsCancellationRequested)
            {
                var task = await _api.GetTaskAsync(created.TaskId, ct);
                if (task == null)
                {
                    ShowToast($"Не удалось получить статус: {_api.LastError ?? "нет ответа"}");
                    AddLog(SimLogLevel.Error, "api", $"GetTask: {_api.LastError ?? "нет ответа"}");
                    break;
                }

                if (task.Status != lastStatus)
                {
                    AddLog(SimLogLevel.Info, "core", $"Статус: {task.Status}");
                    lastStatus = task.Status;
                }

                Progress = MapProgress(task);
                UpdateSimulationStats(task, stopwatch.Elapsed, process, ref lastCpuTime, cpuTimer);

                if (IsTerminal(task.Status))
                {
                    Progress = 1;
                    UpdateSimulationStats(task, stopwatch.Elapsed, process, ref lastCpuTime, cpuTimer);
                    ShowToast($"Завершено: {task.Status}");
                    AddLog(SimLogLevel.Ok, "core",
                        $"Завершено за {FormatElapsed(stopwatch.Elapsed)} (status={task.Status})");

                    // Auto-load results once the simulation finishes.
                    await RefreshResultsAsync();
                    break;
                }

                try { await Task.Delay(750, ct); } catch (TaskCanceledException) { break; }
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Ошибка симуляции: {ex.Message}");
            AddLog(SimLogLevel.Error, "client", ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            cpuTimer.Stop();
            IsRunning = false;
        }
    }

    // ---------- Live simulation telemetry helpers ----------

    private void ResetSimulationStats()
    {
        RunId = "";
        ElapsedLabel = "00:00:00";
        RemainingLabel = "—";
        SpeedLabel = "—";
        CpuLoadLabel = "—";
        ArrivedLabel = "—";
        ServedLabel = "—";
        QueueLabel = "—";
        AvgTimeLabel = "—";
        SimulationLogs.Clear();
        _lastServerLogIndex = 0;
    }

    private void UpdateSimulationStats(
        SimulationTaskDto task,
        TimeSpan elapsed,
        System.Diagnostics.Process process,
        ref TimeSpan lastCpuTime,
        System.Diagnostics.Stopwatch cpuTimer)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        ElapsedLabel = FormatElapsed(elapsed);

        // When progress reaches 100%, always zero out the ETA.
        if (Progress >= 1)
            RemainingLabel = "00:00:00";

        // Speed (iterations / second) and ETA.
        if (task.ResultData != null && elapsed.TotalSeconds > 0.5)
        {
            var current = task.ResultData.CurrentEventsAmount;
            var max = task.ResultData.MaxEventsAmount;
            var speed = current / elapsed.TotalSeconds;
            SpeedLabel = $"{speed.ToString("N0", inv)} итер/с";

            if (Progress < 1 && max > 0 && speed > 0 && current < max)
            {
                var remainingSec = (max - current) / speed;
                RemainingLabel = FormatElapsed(TimeSpan.FromSeconds(remainingSec));
            }

            ArrivedLabel = ((long)current).ToString("N0", inv);
            ServedLabel = task.ResultData.TotalCalls.ToString("N0", inv);

            // Queue length and average service time, derived from per-agent stats.
            double queueAvg = 0;
            int queueAgents = 0;
            double timeAvg = 0;
            int timeAgents = 0;
            foreach (var a in task.ResultData.AgentResults)
            {
                var type = a.AgentType ?? "";
                if (type.Contains("Queue", StringComparison.OrdinalIgnoreCase))
                {
                    // Average expected number of items in queue from state probabilities.
                    double mean = 0;
                    foreach (var kv in a.StatesProbabilities)
                        if (int.TryParse(kv.Key, out var n)) mean += n * kv.Value;
                    queueAvg += mean;
                    queueAgents++;
                }
                if (type.Contains("Service", StringComparison.OrdinalIgnoreCase) ||
                    type.Contains("Block",   StringComparison.OrdinalIgnoreCase))
                {
                    timeAvg += a.Average;
                    timeAgents++;
                }
            }
            QueueLabel = queueAgents > 0
                ? queueAvg.ToString("F2", inv)
                : "0";
            AvgTimeLabel = timeAgents > 0
                ? (timeAvg / timeAgents).ToString("F3", inv)
                : "—";
        }

        // CPU load of the client process — best-effort indicator that work is happening.
        var cpuElapsed = cpuTimer.Elapsed;
        if (cpuElapsed.TotalMilliseconds >= 250)
        {
            try
            {
                process.Refresh();
                var current = process.TotalProcessorTime;
                var cpuUsed = (current - lastCpuTime).TotalMilliseconds;
                var load = cpuUsed / (Environment.ProcessorCount * cpuElapsed.TotalMilliseconds) * 100.0;
                load = Math.Clamp(load, 0, 100);
                CpuLoadLabel = $"{load.ToString("F1", inv)} %";
                lastCpuTime = current;
                cpuTimer.Restart();
            }
            catch { /* ignore */ }
        }

        // Ingest server-side simulation logs into the UI log panel.
        SyncServerLogs(task.ResultData?.Logs);
    }

    private int _lastServerLogIndex;

    private void SyncServerLogs(List<SimulationLogEntryDto>? logs)
    {
        if (logs == null || logs.Count == 0) return;
        for (int i = _lastServerLogIndex; i < logs.Count; i++)
        {
            var e = logs[i];
            var level = e.Level switch
            {
                "ERROR"   => SimLogLevel.Error,
                "WARNING" => SimLogLevel.Warn,
                "SUCCESS" => SimLogLevel.Ok,
                "DEBUG"   => SimLogLevel.Debug,
                _         => SimLogLevel.Info,
            };
            AddLog(level, "core", e.Message);
        }
        _lastServerLogIndex = logs.Count;
    }

    private void AddLog(SimLogLevel level, string source, string message)
    {
        var entry = new SimLogEntry
        {
            Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
            Level = level,
            Source = source,
            Message = message,
        };
        if (Dispatcher.UIThread.CheckAccess())
            SimulationLogs.Add(entry);
        else
            Dispatcher.UIThread.Post(() => SimulationLogs.Add(entry));

        // Keep the log bounded so long runs don't balloon memory.
        const int maxEntries = 500;
        while (SimulationLogs.Count > maxEntries) SimulationLogs.RemoveAt(0);
    }

    private static string FormatElapsed(TimeSpan ts)
        => ts.TotalHours >= 1
            ? ts.ToString(@"hh\:mm\:ss")
            : ts.ToString(@"mm\:ss\.f");

    private static string ShortRunId(string taskId)
    {
        if (string.IsNullOrEmpty(taskId)) return "";
        var s = taskId.Replace("-", "");
        return s.Length <= 6 ? s : s[..6];
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
                Description = WizardDescription,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                ModifiedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Status = ProblemStatus.Draft,
            };

            // Populate agents & edges based on template selection
            switch (WizardTemplate)
            {
                case 0: // Пусто — no agents
                    break;

                case 1: // M/M/1 базовый
                    problem.Agents.AddRange(new[]
                    {
                        new Agent { Id = "src1", Kind = AgentKind.Source,       Name = "Source #1",       X =  60, Y = 200,
                                    ArrivalDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.3 } },
                        new Agent { Id = "svb1", Kind = AgentKind.ServiceBlock, Name = "ServiceBlock #1", X = 360, Y = 200,
                                    Channels = 1,
                                    ServiceDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.5 } },
                        new Agent { Id = "snk1", Kind = AgentKind.Sink,         Name = "Sink",            X = 660, Y = 200 },
                    });
                    problem.Edges.AddRange(new[]
                    {
                        new Edge { Id = "e1", From = "src1", To = "svb1" },
                        new Edge { Id = "e2", From = "svb1", To = "snk1" },
                    });
                    break;

                case 2: // M/M/c с очередью
                    problem.Agents.AddRange(new[]
                    {
                        new Agent { Id = "src1", Kind = AgentKind.Source,       Name = "Source #1",       X =  60, Y = 200,
                                    ArrivalDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.5 } },
                        new Agent { Id = "buf1", Kind = AgentKind.Buffer,       Name = "Queue #1",        X = 240, Y = 200,
                                    Capacity = "50" },
                        new Agent { Id = "svb1", Kind = AgentKind.ServiceBlock, Name = "ServiceBlock #1", X = 460, Y = 200,
                                    Channels = 3,
                                    ServiceDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.4 } },
                        new Agent { Id = "snk1", Kind = AgentKind.Sink,         Name = "Sink",            X = 700, Y = 200 },
                    });
                    problem.Edges.AddRange(new[]
                    {
                        new Edge { Id = "e1", From = "src1", To = "buf1" },
                        new Edge { Id = "e2", From = "buf1", To = "svb1" },
                        new Edge { Id = "e3", From = "svb1", To = "snk1" },
                    });
                    break;

                case 3: // С орбитой повторов
                    problem.Agents.AddRange(new[]
                    {
                        new Agent { Id = "src1", Kind = AgentKind.Source,       Name = "Source #1",       X =  60, Y = 200,
                                    ArrivalDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.3 } },
                        new Agent { Id = "svb1", Kind = AgentKind.ServiceBlock, Name = "ServiceBlock #1", X = 360, Y = 200,
                                    Channels = 1,
                                    ServiceDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.5 } },
                        new Agent { Id = "orb1", Kind = AgentKind.Orbit,        Name = "Orbit #1",        X = 360, Y = 400,
                                    ArrivalDistribution = new() { Kind = DistributionKind.Exponential, Rate = 1.0 } },
                        new Agent { Id = "snk1", Kind = AgentKind.Sink,         Name = "Sink",            X = 660, Y = 200 },
                    });
                    problem.Edges.AddRange(new[]
                    {
                        new Edge { Id = "e1", From = "src1", To  = "svb1" },
                        new Edge { Id = "e2", From = "svb1", To  = "snk1" },
                        new Edge { Id = "e3", From = "svb1", To  = "orb1" },
                        new Edge { Id = "e4", From = "orb1", To  = "svb1" },
                    });
                    break;

                default: // fallback = M/M/1
                    goto case 1;
            }

            var vm = new ProblemViewModel(problem);

            // Always create locally so the user lands in the editor with the new problem.
            problem.Id = $"local-{Guid.NewGuid():N}".Substring(0, 12);
            Problems.Add(vm);
            FilteredProblems.Add(vm);
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
            // Keep whatever is already in the table (e.g. seeded sample data
            // or the last successful fetch) — don't wipe it just because the
            // API is unreachable.
            ShowToast("API недоступен — показаны ранее загруженные данные");
            return;
        }

        try
        {
            // Prefer querying by taskId (more precise); fall back to problemId.
            var taskId = CurrentTaskId;
            var problemId = string.IsNullOrEmpty(taskId) ? CurrentProblem.Model.Id : null;
            var resp = await _api.GetResultsAsync(problemId, taskId);
            if (resp == null)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ResultTable.Clear();
                    RaiseResultsDerivedChanged();
                    ShowToast($"Результаты: {_api.LastError ?? "нет данных"}");
                });
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ResultTable.Clear();

                // Aggregate state probabilities from all results into ResultRow entries
                var probabilities = new Dictionary<int, double>();
                int agentCount = 0;
                foreach (var r in resp.Results)
                {
                    if (r.Data?.AgentResults == null || r.Data.AgentResults.Count == 0)
                        continue;
                    foreach (var agent in r.Data.AgentResults)
                    {
                        agentCount++;
                        foreach (var kv in agent.StatesProbabilities)
                        {
                            if (int.TryParse(kv.Key, out var n))
                                probabilities[n] = probabilities.GetValueOrDefault(n) + kv.Value;
                        }
                    }
                }

                if (probabilities.Count == 0)
                {
                    RaiseResultsDerivedChanged();
                    ShowToast($"Нет данных распределения (results={resp.Results.Length}, agents={agentCount})");
                    return;
                }

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
                OnPropertyChanged(nameof(HasResults));
                RaiseResultsDerivedChanged();
                ShowToast($"Загружено результатов: {resp.Results.Length}");
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShowToast($"Ошибка загрузки результатов: {ex.Message}"));
        }
    }    [RelayCommand] private void ToggleTweaks() => TweaksOpen = !TweaksOpen;

    [RelayCommand]
    private void SetAccent(string hue)
    {
        if (double.TryParse(hue, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var h)) AccentHue = h;
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        if (ResultTable.Count == 0) { ShowToast("Нет данных для экспорта"); return; }

        var topLevel = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow;
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Сохранить результаты в CSV",
            DefaultExtension = "csv",
            SuggestedFileName = $"simq_results_{DateTime.Now:yyyyMMdd_HHmmss}",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } }
            }
        });

        if (file is null) return;

        var sb = new StringBuilder();
        sb.AppendLine("N;P;CDF");
        foreach (var r in ResultTable)
            sb.AppendLine($"{r.N};{r.P:G8};{r.Cdf:G8}");

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(sb.ToString());

        ShowToast("CSV сохранён ✓");
    }

    [RelayCommand]
    private void OpenRunResults(RunRecord? run)
    {
        if (run is null) return;
        // Load results for the selected run
        CurrentTaskId = run.Id;
        _ = RefreshResultsAsync();
        Screen = AppScreen.Results;
    }

    [RelayCommand]
    private void DeleteRun(RunRecord? run)
    {
        if (run is null) return;
        RunHistory.Remove(run);
        ShowToast($"Запуск {run.Id} удалён");
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var topLevel = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow;
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Импорт задачи из JSON",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } },
                FilePickerFileTypes.All
            }
        });

        if (files.Count == 0) return;

        try
        {
            await using var stream = await files[0].OpenReadAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();

            var opts = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var problem = System.Text.Json.JsonSerializer.Deserialize<Problem>(json, opts);
            if (problem is null) { ShowToast("Не удалось разобрать JSON"); return; }

            if (string.IsNullOrEmpty(problem.Id))
                problem.Id = $"imp-{Guid.NewGuid():N}"[..12];

            problem.RebuildEdgeAnchors();

            var vm = new ProblemViewModel(problem);
            Problems.Add(vm);
            FilteredProblems.Add(vm);
            CurrentProblem = vm;
            SelectedAgent = vm.Agents.FirstOrDefault();
            Screen = AppScreen.Editor;
            ShowToast($"Импортировано: {problem.Name}");
        }
        catch (Exception ex)
        {
            ShowToast($"Ошибка импорта: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportProblemAsync()
    {
        var topLevel = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
            ?.MainWindow;
        if (topLevel is null) return;

        var problem = CurrentProblem.Model;
        var suggestedName = string.IsNullOrWhiteSpace(problem.Name) ? "problem" : problem.Name.Trim();

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Экспорт задачи в JSON",
            DefaultExtension = "json",
            SuggestedFileName = suggestedName,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON") { Patterns = new[] { "*.json" } }
            }
        });

        if (file is null) return;

        try
        {
            var opts = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            };
            var json = System.Text.Json.JsonSerializer.Serialize(problem, opts);

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(json);

            ShowToast("Задача экспортирована ✓");
        }
        catch (Exception ex)
        {
            ShowToast($"Ошибка экспорта: {ex.Message}");
        }
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

    // ---------- Summary statistics over ResultTable ----------

    public double Mean => ResultTable.Sum(r => r.N * r.P);

    public double Variance
    {
        get
        {
            var m = Mean;
            return ResultTable.Sum(r => (r.N - m) * (r.N - m) * r.P);
        }
    }

    public int Mode => ResultTable.Count == 0
        ? 0
        : ResultTable.Aggregate((a, b) => a.P >= b.P ? a : b).N;

    public int Median
    {
        get
        {
            double acc = 0;
            foreach (var r in ResultTable)
            {
                acc += r.P;
                if (acc >= 0.5) return r.N;
            }
            return ResultTable.Count == 0 ? 0 : ResultTable[^1].N;
        }
    }

    public double PLeq10 => ResultTable.Where(r => r.N <= 10).Sum(r => r.P);

    private static string Inv(double v, string fmt)
        => v.ToString(fmt, System.Globalization.CultureInfo.InvariantCulture);

    public string MeanLabel     => Inv(Mean,     "F3");
    public string VarianceLabel => Inv(Variance, "F3");
    public string ModeLabel     => Mode.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public string MedianLabel   => Median.ToString(System.Globalization.CultureInfo.InvariantCulture);
    public string PLeq10Label   => Inv(PLeq10,   "F3");

    public string YTick0Label => Inv(0,                     "F3");
    public string YTick1Label => Inv(MaxProbability * 0.25, "F3");
    public string YTick2Label => Inv(MaxProbability * 0.5,  "F3");
    public string YTick3Label => Inv(MaxProbability * 0.75, "F3");
    public string YTick4Label => Inv(MaxProbability,        "F3");

    public string ResultsSubtitle
    {
        get
        {
            var run  = string.IsNullOrEmpty(CurrentTaskId) ? "t-017" : CurrentTaskId;
            var name = CurrentProblem.Name is { Length: > 0 } n ? n : "—";
            return $"{name} · run {run} · {ResultTable.Count} бинов";
        }
    }

    private void RaiseResultsDerivedChanged()
    {
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(MaxProbability));
        OnPropertyChanged(nameof(Mean));
        OnPropertyChanged(nameof(Variance));
        OnPropertyChanged(nameof(Mode));
        OnPropertyChanged(nameof(Median));
        OnPropertyChanged(nameof(PLeq10));
        OnPropertyChanged(nameof(MeanLabel));
        OnPropertyChanged(nameof(VarianceLabel));
        OnPropertyChanged(nameof(ModeLabel));
        OnPropertyChanged(nameof(MedianLabel));
        OnPropertyChanged(nameof(PLeq10Label));
        OnPropertyChanged(nameof(YTick0Label));
        OnPropertyChanged(nameof(YTick1Label));
        OnPropertyChanged(nameof(YTick2Label));
        OnPropertyChanged(nameof(YTick3Label));
        OnPropertyChanged(nameof(YTick4Label));
        OnPropertyChanged(nameof(ResultsSubtitle));
    }

    /// <summary>
    /// Pre-populates the result table with the reference distribution from the
    /// design mock-up (20 bins, μ ≈ 6.302, σ² ≈ 5.760, mode = 6).
    /// </summary>
    private void SeedSampleResults()
    {
        // Raw probabilities from the mock-up; will be normalised to sum = 1.
        var probs = new[]
        {
            5.3132e-3, 1.4543e-2, 3.3464e-2, 6.4728e-2, 1.0525e-1,
            1.4386e-1, 1.6529e-1, 1.5965e-1, 1.2962e-1, 8.8473e-2,
            5.0762e-2, 2.4483e-2, 9.9264e-3, 3.3832e-3, 9.6929e-4,
            2.3345e-4, 4.7263e-5, 8.0438e-6, 1.1508e-6, 1.3840e-7,
        };

        var sum  = probs.Sum();
        var maxP = probs.Max() / sum;

        ResultTable.Clear();
        double cdf = 0;
        for (int k = 0; k < probs.Length; k++)
        {
            var p = probs[k] / sum;
            cdf += p;
            ResultTable.Add(new ResultRow
            {
                N = k,
                P = p,
                Cdf = cdf,
                BarWidth    = maxP > 0 ? p / maxP * 220 : 0,
                ChartHeight = maxP > 0 ? p / maxP * 200 : 0,
            });
        }

        RaiseResultsDerivedChanged();
    }
}
