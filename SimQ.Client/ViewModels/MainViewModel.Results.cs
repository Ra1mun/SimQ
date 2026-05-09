using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using SimQ.Client.Models;
using SimQ.Client.Services;

namespace SimQ.Client.ViewModels;

public partial class MainViewModel
{
    public bool HasResults => ResultTable.Count > 0;

    public double MaxProbability => ResultTable.Count == 0 ? 1 : ResultTable.Max(r => r.P);

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
        => v.ToString(fmt, CultureInfo.InvariantCulture);

    public string MeanLabel     => Inv(Mean,     "F3");
    public string VarianceLabel => Inv(Variance, "F3");
    public string ModeLabel     => Mode.ToString(CultureInfo.InvariantCulture);
    public string MedianLabel   => Median.ToString(CultureInfo.InvariantCulture);
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

    private async Task RefreshResultsAsync()
    {
        if (_api == null || !IsApiOnline)
        {
            ShowError("API недоступен — показаны ранее загруженные данные");
            return;
        }

        try
        {
            var taskId    = CurrentTaskId;
            var problemId = string.IsNullOrEmpty(taskId) ? CurrentProblem.Model.Id : null;
            var resp = await _api.GetResultsAsync(problemId, taskId);
            if (resp == null)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ResultTable.Clear();
                    RaiseResultsDerivedChanged();
                    ShowError($"Результаты: {_api.LastError ?? "нет данных"}");
                });
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ResultTable.Clear();
                
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
                    ShowError($"Нет данных распределения (results={resp.Results.Length}, agents={agentCount})");
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
                        N           = kv.Key,
                        P           = p,
                        Cdf         = cdf,
                        BarWidth    = maxP > 0 ? p / maxP * 220 : 0,
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
            await Dispatcher.UIThread.InvokeAsync(() => ShowError($"Ошибка загрузки результатов: {ex.Message}"));
        }
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        if (ResultTable.Count == 0)
        {
            ShowError("Нет данных для экспорта");
            return;
        }

        var file = await StoragePicker.SaveAsync(
            "Сохранить результаты в CSV",
            "csv",
            $"simq_results_{DateTime.Now:yyyyMMdd_HHmmss}",
            StoragePicker.Csv);
        if (file is null) return;

        var sb = new StringBuilder();
        sb.AppendLine("N;P;CDF");
        foreach (var r in ResultTable)
            sb.AppendLine($"{r.N};{r.P:G8};{r.Cdf:G8}");

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(sb.ToString());

        ShowToast("CSV сохранён");
    }
    
    private void SeedSampleResults()
    {
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
                N           = k,
                P           = p,
                Cdf         = cdf,
                BarWidth    = maxP > 0 ? p / maxP * 220 : 0,
                ChartHeight = maxP > 0 ? p / maxP * 200 : 0,
            });
        }

        RaiseResultsDerivedChanged();
    }
}
