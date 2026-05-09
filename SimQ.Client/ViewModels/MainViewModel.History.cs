using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using SimQ.Client.Models;

namespace SimQ.Client.ViewModels;

public partial class MainViewModel
{
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
                        Id          = t.TaskId,
                        ProblemId   = "",
                        ProblemName = t.TaskId,
                        Status = t.Status switch
                        {
                            "Completed" => RunStatus.Done,
                            "Error"     => RunStatus.Failed,
                            "Canceled"  => RunStatus.Cancelled,
                            _           => RunStatus.Running,
                        },
                        StartedAt = t.Started?.ToString("yyyy-MM-dd HH:mm") ?? "",
                        Duration  = (t.Started.HasValue && t.Finished.HasValue)
                            ? (t.Finished.Value - t.Started.Value).ToString(@"hh\:mm\:ss") : "",
                        Iterations = (int)(t.ResultData?.CurrentEventsAmount ?? 0),
                    });
                }
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShowError($"История недоступна: {ex.Message}"));
        }
    }

    [RelayCommand]
    private void OpenRunResults(RunRecord? run)
    {
        if (run is null) return;
        
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
}
