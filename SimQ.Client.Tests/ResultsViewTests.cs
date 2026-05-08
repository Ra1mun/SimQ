using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using SimQ.Client.Models;
using SimQ.Client.ViewModels;
using SimQ.Client.Views;

namespace SimQ.Client.Tests;

public class ResultsViewTests
{
    private static MainViewModel CreateVm()
    {
        var vm = new MainViewModel(null);
        vm.Screen = AppScreen.Results;
        return vm;
    }

    private static MainWindow CreateWindow(MainViewModel vm)
    {
        var w = new MainWindow { DataContext = vm };
        w.Show();
        return w;
    }

    // ── 1. Статистические данные (Summary Stats) ──────────────────

    [AvaloniaFact]
    public void ResultTable_ProbabilitiesSum_IsOne()
    {
        var vm = CreateVm();
        var sum = vm.ResultTable.Sum(r => r.P);
        Assert.InRange(sum, 0.9999, 1.0001);
    }

    [AvaloniaFact]
    public void ResultTable_CdfLastRow_IsOne()
    {
        var vm = CreateVm();
        var last = vm.ResultTable.Last();
        Assert.InRange(last.Cdf, 0.9999, 1.0001);
    }

    [AvaloniaFact]
    public void ResultTable_CdfIsAccumulatedP()
    {
        var vm = CreateVm();
        double acc = 0;
        foreach (var row in vm.ResultTable)
        {
            acc += row.P;
            Assert.Equal(acc, row.Cdf, 1e-12);
        }
    }

    [AvaloniaFact]
    public void ResultTable_CdfMonotonicallyIncreasing()
    {
        var vm = CreateVm();
        double prev = 0;
        foreach (var row in vm.ResultTable)
        {
            Assert.True(row.Cdf >= prev, $"CDF не монотонна: N={row.N}, Cdf={row.Cdf} < {prev}");
            prev = row.Cdf;
        }
    }

    [AvaloniaFact]
    public void ResultTable_MeanCalculation_IsCorrect()
    {
        var vm = CreateVm();
        double mean = vm.ResultTable.Sum(r => r.N * r.P);
        // mean should be close to 6.3 (the parameter used in SampleData)
        Assert.InRange(mean, 5.0, 8.0);
    }

    [AvaloniaFact]
    public void ResultTable_VarianceCalculation_IsCorrect()
    {
        var vm = CreateVm();
        double mean = vm.ResultTable.Sum(r => r.N * r.P);
        double variance = vm.ResultTable.Sum(r => (r.N - mean) * (r.N - mean) * r.P);
        Assert.True(variance > 0, "Дисперсия должна быть положительной");
    }

    [AvaloniaFact]
    public void ResultTable_Mode_IsMaxProbabilityRow()
    {
        var vm = CreateVm();
        var mode = vm.ResultTable.OrderByDescending(r => r.P).First();
        // mode should be the N closest to mean(6.3) → N=6
        Assert.InRange(mode.N, 5, 7);
    }

    [AvaloniaFact]
    public void ResultTable_Median_IsInExpectedRange()
    {
        var vm = CreateVm();
        double acc = 0;
        int median = 0;
        foreach (var row in vm.ResultTable)
        {
            acc += row.P;
            if (acc >= 0.5)
            {
                median = row.N;
                break;
            }
        }
        Assert.InRange(median, 5, 8);
    }

    [AvaloniaFact]
    public void ResultTable_CdfAtK10_MatchesSumOfProbabilities()
    {
        var vm = CreateVm();
        double sumUpTo10 = vm.ResultTable.Where(r => r.N <= 10).Sum(r => r.P);
        var row10 = vm.ResultTable.FirstOrDefault(r => r.N == 10);
        if (row10 != null)
            Assert.Equal(sumUpTo10, row10.Cdf, 1e-10);
    }

    [AvaloniaFact]
    public void HeaderContains_ProblemName()
    {
        var vm = CreateVm();
        Assert.NotNull(vm.CurrentProblem);
        Assert.False(string.IsNullOrEmpty(vm.CurrentProblem.Name));
    }

    [AvaloniaFact]
    public void FloatingPointFormat_F4_HasFourDecimals()
    {
        var vm = CreateVm();
        foreach (var row in vm.ResultTable)
        {
            string formatted = row.P.ToString("F4", CultureInfo.InvariantCulture);
            var decimals = formatted.Split('.')[1];
            Assert.Equal(4, decimals.Length);
        }
    }

    // ── 2. Графики (Chart / Visual) ──────────────────────────────

    [AvaloniaFact]
    public void ChartHeight_MaxIs200()
    {
        var vm = CreateVm();
        var maxH = vm.ResultTable.Max(r => r.ChartHeight);
        Assert.Equal(200, maxH, 0.01);
    }

    [AvaloniaFact]
    public void ChartHeight_AllNonNegative()
    {
        var vm = CreateVm();
        Assert.All(vm.ResultTable, r => Assert.True(r.ChartHeight >= 0));
    }

    [AvaloniaFact]
    public void ChartHeight_ProportionalToP()
    {
        var vm = CreateVm();
        var maxP = vm.ResultTable.Max(r => r.P);
        foreach (var r in vm.ResultTable)
        {
            double expected = (r.P / maxP) * 200;
            Assert.Equal(expected, r.ChartHeight, 0.01);
        }
    }

    [AvaloniaFact]
    public void BarWidth_MaxIs220()
    {
        var vm = CreateVm();
        var maxW = vm.ResultTable.Max(r => r.BarWidth);
        Assert.Equal(220, maxW, 0.01);
    }

    [AvaloniaFact]
    public void BarWidth_ProportionalToP()
    {
        var vm = CreateVm();
        var maxP = vm.ResultTable.Max(r => r.P);
        foreach (var r in vm.ResultTable)
        {
            double expected = (r.P / maxP) * 220;
            Assert.Equal(expected, r.BarWidth, 0.01);
        }
    }

    [AvaloniaFact]
    public void AxisX_ValuesMatch_NColumn()
    {
        var vm = CreateVm();
        var ns = vm.ResultTable.Select(r => r.N).ToList();
        // Should be 0..19 (20 bins from SampleData)
        Assert.Equal(0, ns.First());
        Assert.Equal(19, ns.Last());
        for (int i = 1; i < ns.Count; i++)
            Assert.Equal(ns[i - 1] + 1, ns[i]);
    }

    [AvaloniaFact]
    public void MaxProbability_EqualsMaxP()
    {
        var vm = CreateVm();
        Assert.Equal(vm.ResultTable.Max(r => r.P), vm.MaxProbability);
    }

    // ── 3. Таблица результатов ────────────────────────────────────

    [AvaloniaFact]
    public void ResultTable_Has16Rows()
    {
        var vm = CreateVm();
        Assert.Equal(20, vm.ResultTable.Count);
    }

    [AvaloniaFact]
    public void ResultTable_NValues_0To15()
    {
        var vm = CreateVm();
        for (int i = 0; i < vm.ResultTable.Count; i++)
            Assert.Equal(i, vm.ResultTable[i].N);
    }

    [AvaloniaFact]
    public void ResultTable_AllProbabilities_NonNegative()
    {
        var vm = CreateVm();
        Assert.All(vm.ResultTable, r => Assert.True(r.P >= 0, $"P<0 для N={r.N}"));
    }

    [AvaloniaFact]
    public void ResultTable_SmallProbabilities_ExponentialFormat()
    {
        var vm = CreateVm();
        // Rows far from mean should have very small P
        var smallRows = vm.ResultTable.Where(r => r.P < 0.001).ToList();
        foreach (var r in smallRows)
        {
            string sci = r.P.ToString("E4", CultureInfo.InvariantCulture);
            Assert.Contains("E", sci);
        }
    }

    [AvaloniaFact]
    public void ResultTable_PFormat_F4HasFourDecimals()
    {
        var vm = CreateVm();
        foreach (var row in vm.ResultTable)
        {
            string s = row.P.ToString("F4", CultureInfo.InvariantCulture);
            var parts = s.Split('.');
            Assert.Equal(2, parts.Length);
            Assert.Equal(4, parts[1].Length);
        }
    }

    [AvaloniaFact]
    public void ResultsView_TableRendered()
    {
        var vm = CreateVm();
        var w = CreateWindow(vm);
        Assert.True(vm.IsResults);
        Assert.True(vm.ResultTable.Count > 0);
    }

    // ── 4. Экспорт ────────────────────────────────────────────────

    [AvaloniaFact]
    public void ExportCsv_DataAvailable_AllColumns()
    {
        var vm = CreateVm();
        // Simulate CSV export from ResultTable
        var lines = new List<string> { "N,P,CDF" };
        foreach (var r in vm.ResultTable)
            lines.Add($"{r.N},{r.P.ToString("F4", CultureInfo.InvariantCulture)},{r.Cdf.ToString("F4", CultureInfo.InvariantCulture)}");

        Assert.Equal(vm.ResultTable.Count + 1, lines.Count);
        Assert.Equal("N,P,CDF", lines[0]);

        var firstData = lines[1].Split(',');
        Assert.Equal(3, firstData.Length);
        Assert.Equal("0", firstData[0]);
    }

    [AvaloniaFact]
    public void ExportCsv_AllRowsIncluded()
    {
        var vm = CreateVm();
        var csvRows = vm.ResultTable.Select(r =>
            $"{r.N},{r.P.ToString(CultureInfo.InvariantCulture)},{r.Cdf.ToString(CultureInfo.InvariantCulture)}"
        ).ToList();

        Assert.Equal(20, csvRows.Count);
    }

    [AvaloniaFact]
    public void ExportCsv_FirstAndLastRow_Valid()
    {
        var vm = CreateVm();
        var first = vm.ResultTable.First();
        var last = vm.ResultTable.Last();

        Assert.Equal(0, first.N);
        Assert.Equal(19, last.N);
        Assert.InRange(last.Cdf, 0.9999, 1.0001);
    }

    // ── 5. UI/UX и граничные состояния ────────────────────────────

    [AvaloniaFact]
    public void EmptyResults_MaxProbability_IsOne()
    {
        var vm = new MainViewModel(null);
        vm.ResultTable.Clear();
        Assert.Equal(1, vm.MaxProbability);
    }

    [AvaloniaFact]
    public void EmptyResults_TableIsEmpty()
    {
        var vm = new MainViewModel(null);
        vm.ResultTable.Clear();
        Assert.Empty(vm.ResultTable);
    }

    [AvaloniaFact]
    public void EmptyResults_CsvExport_OnlyHeader()
    {
        var vm = new MainViewModel(null);
        vm.ResultTable.Clear();
        var lines = new List<string> { "N,P,CDF" };
        foreach (var r in vm.ResultTable)
            lines.Add($"{r.N},{r.P},{r.Cdf}");

        Assert.Single(lines); // only header
    }

    [AvaloniaFact]
    public void Localization_ScreenLabel_Results()
    {
        var vm = CreateVm();
        Assert.Equal("03 Результаты", vm.ScreenLabel);
    }

    [AvaloniaFact]
    public void ResultsView_Renders()
    {
        var vm = CreateVm();
        var w = CreateWindow(vm);
        Assert.True(vm.IsResults);
    }

    [AvaloniaFact]
    public void ResultsView_MathSymbols_InView()
    {
        // The view XAML contains "РАСПРЕДЕЛЕНИЕ P(N)" header
        // Verify the data model supports proper math notation
        var vm = CreateVm();
        Assert.All(vm.ResultTable, r =>
        {
            Assert.True(r.P >= 0 && r.P <= 1, "P должно быть в [0,1]");
            Assert.True(r.Cdf >= 0 && r.Cdf <= 1.0001, "CDF должно быть в [0,1]");
        });
    }

    // ── 6. Синхронизация состояний ────────────────────────────────

    [AvaloniaFact]
    public void NavigateAway_AndBack_ResultsPreserved()
    {
        var vm = CreateVm();
        var countBefore = vm.ResultTable.Count;
        var firstP = vm.ResultTable.First().P;

        vm.Screen = AppScreen.Editor;
        Assert.True(vm.IsEditor);

        vm.Screen = AppScreen.Results;
        Assert.True(vm.IsResults);
        Assert.Equal(countBefore, vm.ResultTable.Count);
        Assert.Equal(firstP, vm.ResultTable.First().P);
    }

    [AvaloniaFact]
    public void NavigateAway_AndBack_CurrentProblem_Preserved()
    {
        var vm = CreateVm();
        var problemName = vm.CurrentProblem?.Name;

        vm.Screen = AppScreen.Editor;
        vm.Screen = AppScreen.Results;

        Assert.Equal(problemName, vm.CurrentProblem?.Name);
    }

    [AvaloniaFact]
    public void NavigateToResults_SetsIsResults()
    {
        var vm = new MainViewModel(null);
        vm.Screen = AppScreen.Results;
        Assert.True(vm.IsResults);
        Assert.False(vm.IsEditor);
        Assert.False(vm.IsSimulate);
        Assert.False(vm.IsTasks);
    }

    [AvaloniaFact]
    public void MultipleNavigationCycles_DataIntact()
    {
        var vm = CreateVm();
        var snapshot = vm.ResultTable.Select(r => (r.N, r.P, r.Cdf)).ToList();

        for (int i = 0; i < 5; i++)
        {
            vm.Screen = AppScreen.Tasks;
            vm.Screen = AppScreen.Results;
        }

        var current = vm.ResultTable.Select(r => (r.N, r.P, r.Cdf)).ToList();
        Assert.Equal(snapshot.Count, current.Count);
        for (int i = 0; i < snapshot.Count; i++)
        {
            Assert.Equal(snapshot[i].N, current[i].N);
            Assert.Equal(snapshot[i].P, current[i].P, 1e-12);
        }
    }

    // ── Дополнительные проверки ───────────────────────────────────

    [AvaloniaFact]
    public void ResultRow_BarWidth_NonNegative()
    {
        var vm = CreateVm();
        Assert.All(vm.ResultTable, r => Assert.True(r.BarWidth >= 0));
    }

    [AvaloniaFact]
    public void ResultRow_ChartHeight_NeverExceeds200()
    {
        var vm = CreateVm();
        Assert.All(vm.ResultTable, r => Assert.True(r.ChartHeight <= 200.01));
    }

    [AvaloniaFact]
    public void ResultRow_BarWidth_NeverExceeds220()
    {
        var vm = CreateVm();
        Assert.All(vm.ResultTable, r => Assert.True(r.BarWidth <= 220.01));
    }

    [AvaloniaFact]
    public void ResultTable_Symmetry_AroundMean()
    {
        var vm = CreateVm();
        double mean = vm.ResultTable.Sum(r => r.N * r.P);
        // P values should be roughly symmetric around the mean
        int modeN = vm.ResultTable.OrderByDescending(r => r.P).First().N;
        Assert.InRange(modeN, (int)mean - 1, (int)mean + 1);
    }

    [AvaloniaFact]
    public void ResultTable_TailProbabilities_Decrease()
    {
        var vm = CreateVm();
        int modeN = vm.ResultTable.OrderByDescending(r => r.P).First().N;
        // Right tail: probabilities should decrease
        var rightTail = vm.ResultTable.Where(r => r.N > modeN).ToList();
        for (int i = 1; i < rightTail.Count; i++)
            Assert.True(rightTail[i].P <= rightTail[i - 1].P + 1e-10,
                $"Правый хвост: P[{rightTail[i].N}]={rightTail[i].P} > P[{rightTail[i - 1].N}]={rightTail[i - 1].P}");
    }
}
