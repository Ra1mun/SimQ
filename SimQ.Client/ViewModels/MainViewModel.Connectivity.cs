using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SimQ.Client.Services;

namespace SimQ.Client.ViewModels;

public partial class MainViewModel
{
    private readonly SimQApiClient? _api;
    private CancellationTokenSource? _healthCts;

    [ObservableProperty] private bool _isApiOnline;
    [ObservableProperty] private string _apiStatusText = "SimQ.Core: проверка…";
    [ObservableProperty] private string _apiBaseUrl;
    [ObservableProperty] private string? _currentTaskId;

    public string EndpointTaskIdLabel
        => string.IsNullOrEmpty(CurrentTaskId)
            ? CurrentProblem.Model.Id
            : CurrentTaskId;

    partial void OnCurrentTaskIdChanged(string? value)
    {
        OnPropertyChanged(nameof(EndpointTaskIdLabel));
        OnPropertyChanged(nameof(ResultsSubtitle));
    }

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
                    await Dispatcher.UIThread.InvokeAsync(() => ShowError($"API: {ex.Message}"));
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
                SelectedAgent  = CurrentProblem.Agents.FirstOrDefault();
            }
            else
            {
                var empty = new ProblemViewModel(CreateEmptyProblemPlaceholder());
                Problems.Add(empty);
                FilteredProblems.Add(empty);
                CurrentProblem = empty;
                SelectedAgent  = null;
            }

            ShowToast($"API: задач на сервере — {list.Total}");
        });
        
        await RefreshHistoryAsync();
    }

    private static Models.Problem CreateEmptyProblemPlaceholder() => new()
    {
        Id          = "",
        Name        = "—",
        Description = "Нет загруженных задач",
        CreatedAt   = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
        ModifiedAt  = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
        Status      = Models.ProblemStatus.Draft,
    };
}
