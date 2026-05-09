using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimQ.Client.Models;
using SimQ.Client.Services;

namespace SimQ.Client.ViewModels;

public partial class MainViewModel
{
    private CancellationTokenSource? _simulationCts;
    private int _lastServerLogIndex;

    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private int _iterations = 50000;
    [ObservableProperty] private int _seed = 42;

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

    public string IterationsLabel => $"{(int)(Progress * Iterations):N0} / {Iterations:N0} итераций";
    public string ProgressPercent => $"{Progress * 100:F1}";

    partial void OnProgressChanged(double value)
    {
        OnPropertyChanged(nameof(IterationsLabel));
        OnPropertyChanged(nameof(ProgressPercent));
    }

    partial void OnIterationsChanged(int value)
    {
        OnPropertyChanged(nameof(IterationsLabel));
    }

    [RelayCommand]
    private async Task StartSimulationAsync()
    {
        if (IsRunning) return;

        if (_api == null || !IsApiOnline)
        {
            ShowError("API недоступен — запуск отменён");
            return;
        }

        if (string.IsNullOrEmpty(CurrentProblem.Model.Id) ||
            CurrentProblem.Model.Id.StartsWith("p-", StringComparison.Ordinal))
        {
            ShowError("Сначала сохраните задачу (Сохранить)");
            return;
        }

        IsRunning = true;
        Progress = 0;
        CurrentTaskId = null;
        ResetSimulationStats();
        _simulationCts = new CancellationTokenSource();
        var ct = _simulationCts.Token;

        var stopwatch = Stopwatch.StartNew();
        var cpuTimer  = new Stopwatch();
        var process   = Process.GetCurrentProcess();
        var lastCpuTime = process.TotalProcessorTime;
        cpuTimer.Start();

        AddLog(SimLogLevel.Info, "client", $"Запуск моделирования (iter={Iterations:N0})");

        try
        {
            var created = await _api.CreateTaskAsync(new CreateTaskRequestDto
            {
                ProblemId = CurrentProblem.Model.Id,
                MaxSteps  = (uint)Math.Max(1, Iterations),
            }, ct);

            if (created == null || string.IsNullOrEmpty(created.TaskId))
            {
                ShowError($"Не удалось создать задачу: {_api.LastError ?? "нет ответа"}");
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
                    ShowError($"Не удалось получить статус: {_api.LastError ?? "нет ответа"}");
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
            ShowError($"Ошибка симуляции: {ex.Message}");
            AddLog(SimLogLevel.Error, "client", ex.Message);
        }
        finally
        {
            stopwatch.Stop();
            cpuTimer.Stop();
            IsRunning = false;
        }
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

    // ---------- Telemetry helpers ----------

    private void ResetSimulationStats()
    {
        RunId          = "";
        ElapsedLabel   = "00:00:00";
        RemainingLabel = "—";
        SpeedLabel     = "—";
        CpuLoadLabel   = "—";
        ArrivedLabel   = "—";
        ServedLabel    = "—";
        QueueLabel     = "—";
        AvgTimeLabel   = "—";
        SimulationLogs.Clear();
        _lastServerLogIndex = 0;
    }

    private void UpdateSimulationStats(
        SimulationTaskDto task,
        TimeSpan elapsed,
        Process process,
        ref TimeSpan lastCpuTime,
        Stopwatch cpuTimer)
    {
        var inv = CultureInfo.InvariantCulture;
        ElapsedLabel = FormatElapsed(elapsed);

        if (Progress >= 1)
            RemainingLabel = "00:00:00";

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
            ServedLabel  = task.ResultData.TotalCalls.ToString("N0", inv);

            double queueAvg = 0;
            int queueAgents = 0;
            double timeAvg = 0;
            int timeAgents = 0;
            foreach (var a in task.ResultData.AgentResults)
            {
                var type = a.AgentType ?? "";
                if (type.Contains("Queue", StringComparison.OrdinalIgnoreCase))
                {
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

        SyncServerLogs(task.ResultData?.Logs);
    }

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
            Level     = level,
            Source    = source,
            Message   = message,
        };
        if (Dispatcher.UIThread.CheckAccess())
            SimulationLogs.Add(entry);
        else
            Dispatcher.UIThread.Post(() => SimulationLogs.Add(entry));

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
}
