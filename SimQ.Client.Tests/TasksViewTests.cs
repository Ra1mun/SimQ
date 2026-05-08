using System.Globalization;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using SimQ.Client.Converters;
using SimQ.Client.Models;
using SimQ.Client.ViewModels;
using SimQ.Client.Views;

namespace SimQ.Client.Tests;

/// <summary>
/// Тесты экрана «История запусков»: отображение данных, статусы,
/// фильтрация, навигация, UI/локализация, негативные сценарии.
/// </summary>
public class TasksViewTests
{
    private static (MainWindow window, MainViewModel vm) CreateTasks()
    {
        var vm = new MainViewModel(null);
        var window = new MainWindow { DataContext = vm, Width = 1440, Height = 900 };
        window.Show();
        vm.SelectScreenCommand.Execute("Tasks");
        return (window, vm);
    }

    // ═════════════════════════════════════════════════════════
    //  1. ОТОБРАЖЕНИЕ ДАННЫХ (Table View)
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Количество записей = 7.
    /// </summary>
    [AvaloniaFact]
    public void Table_RecordCount_Is7()
    {
        var (_, vm) = CreateTasks();

        Assert.Equal(7, vm.RunHistory.Count);
    }

    /// <summary>
    /// Сценарий: Все записи содержат непустой ID.
    /// </summary>
    [AvaloniaFact]
    public void Table_AllRecords_HaveId()
    {
        var (_, vm) = CreateTasks();

        foreach (var r in vm.RunHistory)
            Assert.False(string.IsNullOrEmpty(r.Id));
    }

    /// <summary>
    /// Сценарий: Все записи содержат имя задачи.
    /// </summary>
    [AvaloniaFact]
    public void Table_AllRecords_HaveProblemName()
    {
        var (_, vm) = CreateTasks();

        foreach (var r in vm.RunHistory)
            Assert.False(string.IsNullOrEmpty(r.ProblemName));
    }

    /// <summary>
    /// Сценарий: Статусы записей — Done, Failed, Cancelled.
    /// </summary>
    [AvaloniaFact]
    public void Table_Statuses_Correct()
    {
        var (_, vm) = CreateTasks();

        var statuses = vm.RunHistory.Select(r => r.Status).Distinct().OrderBy(s => s).ToList();
        Assert.Contains(RunStatus.Done, statuses);
        Assert.Contains(RunStatus.Failed, statuses);
        Assert.Contains(RunStatus.Cancelled, statuses);
    }

    /// <summary>
    /// Сценарий: Количество завершённых = 5, ошибок = 1, отменённых = 1.
    /// </summary>
    [AvaloniaFact]
    public void Table_StatusCounts()
    {
        var (_, vm) = CreateTasks();

        Assert.Equal(5, vm.RunHistory.Count(r => r.Status == RunStatus.Done));
        Assert.Equal(1, vm.RunHistory.Count(r => r.Status == RunStatus.Failed));
        Assert.Equal(1, vm.RunHistory.Count(r => r.Status == RunStatus.Cancelled));
    }

    /// <summary>
    /// Сценарий: Формат даты запуска — ГГГГ-ММ-ДД ЧЧ:ММ.
    /// </summary>
    [AvaloniaFact]
    public void Table_DateFormat()
    {
        var (_, vm) = CreateTasks();

        foreach (var r in vm.RunHistory)
        {
            Assert.False(string.IsNullOrEmpty(r.StartedAt));
            Assert.Matches(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}", r.StartedAt);
        }
    }

    /// <summary>
    /// Сценарий: Длительность — формат ЧЧ:ММ:СС для завершённых.
    /// </summary>
    [AvaloniaFact]
    public void Table_DurationFormat()
    {
        var (_, vm) = CreateTasks();

        var done = vm.RunHistory.Where(r => r.Status == RunStatus.Done).ToList();
        foreach (var r in done)
        {
            Assert.False(string.IsNullOrEmpty(r.Duration));
            Assert.Matches(@"\d{2}:\d{2}:\d{2}", r.Duration);
        }
    }

    /// <summary>
    /// Сценарий: Итерации > 0 для завершённых, 0 для ошибок/отмен.
    /// </summary>
    [AvaloniaFact]
    public void Table_Iterations_CorrectPerStatus()
    {
        var (_, vm) = CreateTasks();

        foreach (var r in vm.RunHistory)
        {
            if (r.Status == RunStatus.Done)
                Assert.True(r.Iterations > 0, $"{r.Id} should have iterations > 0");
            else
                Assert.Equal(0, r.Iterations);
        }
    }

    /// <summary>
    /// Сценарий: Записи имеют связь с ProblemId.
    /// </summary>
    [AvaloniaFact]
    public void Table_Records_HaveProblemId()
    {
        var (_, vm) = CreateTasks();

        foreach (var r in vm.RunHistory)
            Assert.False(string.IsNullOrEmpty(r.ProblemId));
    }

    /// <summary>
    /// Сценарий: Note для каждого статуса.
    /// </summary>
    [AvaloniaFact]
    public void Table_Note_PerStatus()
    {
        var done = new RunRecord { Status = RunStatus.Done, Duration = "00:04:11" };
        Assert.Contains("длительность", done.Note);
        Assert.Contains("00:04:11", done.Note);

        var failed = new RunRecord { Status = RunStatus.Failed };
        Assert.Contains("не удалось", failed.Note);

        var cancelled = new RunRecord { Status = RunStatus.Cancelled };
        Assert.Contains("остановлено", cancelled.Note);

        var running = new RunRecord { Status = RunStatus.Running };
        Assert.Contains("выполняется", running.Note);
    }

    // ═════════════════════════════════════════════════════════
    //  2. КОНВЕРТЕРЫ СТАТУСОВ (Цветовая индикация)
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: RunStatusToBrushConverter — завершён = зелёный (#2D8A4F).
    /// </summary>
    [AvaloniaFact]
    public void Converter_Done_GreenBrush()
    {
        var brush = RunStatusToBrushConverter.Instance.Convert(
            RunStatus.Done, typeof(IBrush), null, CultureInfo.InvariantCulture) as SolidColorBrush;

        Assert.NotNull(brush);
        Assert.Equal(Color.Parse("#2D8A4F"), brush!.Color);
    }

    /// <summary>
    /// Сценарий: RunStatusToBrushConverter — ошибка = красный (#C53030).
    /// </summary>
    [AvaloniaFact]
    public void Converter_Failed_RedBrush()
    {
        var brush = RunStatusToBrushConverter.Instance.Convert(
            RunStatus.Failed, typeof(IBrush), null, CultureInfo.InvariantCulture) as SolidColorBrush;

        Assert.NotNull(brush);
        Assert.Equal(Color.Parse("#C53030"), brush!.Color);
    }

    /// <summary>
    /// Сценарий: RunStatusToBrushConverter — отменён = серый (#828998).
    /// </summary>
    [AvaloniaFact]
    public void Converter_Cancelled_GrayBrush()
    {
        var brush = RunStatusToBrushConverter.Instance.Convert(
            RunStatus.Cancelled, typeof(IBrush), null, CultureInfo.InvariantCulture) as SolidColorBrush;

        Assert.NotNull(brush);
        Assert.Equal(Color.Parse("#828998"), brush!.Color);
    }

    /// <summary>
    /// Сценарий: RunStatusToBrushConverter — выполняется = синий (#3D5FCC).
    /// </summary>
    [AvaloniaFact]
    public void Converter_Running_BlueBrush()
    {
        var brush = RunStatusToBrushConverter.Instance.Convert(
            RunStatus.Running, typeof(IBrush), null, CultureInfo.InvariantCulture) as SolidColorBrush;

        Assert.NotNull(brush);
        Assert.Equal(Color.Parse("#3D5FCC"), brush!.Color);
    }

    /// <summary>
    /// Сценарий: RunStatusToLabelConverter — русские метки.
    /// </summary>
    [AvaloniaFact]
    public void Converter_StatusLabels_Russian()
    {
        var conv = RunStatusToLabelConverter.Instance;
        var ci = CultureInfo.InvariantCulture;

        Assert.Equal("завершён",    conv.Convert(RunStatus.Done,      typeof(string), null, ci));
        Assert.Equal("ошибка",      conv.Convert(RunStatus.Failed,    typeof(string), null, ci));
        Assert.Equal("отменён",     conv.Convert(RunStatus.Cancelled, typeof(string), null, ci));
        Assert.Equal("выполняется", conv.Convert(RunStatus.Running,   typeof(string), null, ci));
    }

    /// <summary>
    /// Сценарий: ProblemStatusToLabelConverter — русские метки.
    /// </summary>
    [AvaloniaFact]
    public void Converter_ProblemStatusLabels_Russian()
    {
        var conv = ProblemStatusToLabelConverter.Instance;
        var ci = CultureInfo.InvariantCulture;

        Assert.Equal("черновик", conv.Convert(ProblemStatus.Draft,   typeof(string), null, ci));
        Assert.Equal("готово",   conv.Convert(ProblemStatus.Ready,   typeof(string), null, ci));
        Assert.Equal("идёт",     conv.Convert(ProblemStatus.Running, typeof(string), null, ci));
        Assert.Equal("ошибка",   conv.Convert(ProblemStatus.Failed,  typeof(string), null, ci));
    }

    /// <summary>
    /// Сценарий: ProblemStatusToBrushConverter — черновик = оранжевый (#B07A1A).
    /// </summary>
    [AvaloniaFact]
    public void Converter_Draft_OrangeBrush()
    {
        var brush = ProblemStatusToBrushConverter.Instance.Convert(
            ProblemStatus.Draft, typeof(IBrush), null, CultureInfo.InvariantCulture) as SolidColorBrush;

        Assert.NotNull(brush);
        Assert.Equal(Color.Parse("#B07A1A"), brush!.Color);
    }

    /// <summary>
    /// Сценарий: BoolToOnlineBrushConverter — online=зелёный, offline=красный.
    /// </summary>
    [AvaloniaFact]
    public void Converter_OnlineOffline_Brushes()
    {
        var ci = CultureInfo.InvariantCulture;
        var online = BoolToOnlineBrushConverter.Instance.Convert(true, typeof(IBrush), null, ci) as SolidColorBrush;
        var offline = BoolToOnlineBrushConverter.Instance.Convert(false, typeof(IBrush), null, ci) as SolidColorBrush;

        Assert.Equal(Color.Parse("#2D8A4F"), online!.Color);
        Assert.Equal(Color.Parse("#C53030"), offline!.Color);
    }

    // ═════════════════════════════════════════════════════════
    //  3. НАВИГАЦИЯ И ПЕРЕХОДЫ
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Экран Tasks — ScreenLabel содержит «История».
    /// </summary>
    [AvaloniaFact]
    public void Navigation_ScreenLabel()
    {
        var (_, vm) = CreateTasks();

        Assert.Contains("История", vm.ScreenLabel);
    }

    /// <summary>
    /// Сценарий: Переход из Tasks в Results.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_ToResults()
    {
        var (_, vm) = CreateTasks();

        vm.SelectScreenCommand.Execute("Results");

        Assert.Equal(AppScreen.Results, vm.Screen);
    }

    /// <summary>
    /// Сценарий: Переход из Tasks в Editor.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_ToEditor()
    {
        var (_, vm) = CreateTasks();

        vm.SelectScreenCommand.Execute("Editor");

        Assert.Equal(AppScreen.Editor, vm.Screen);
    }

    /// <summary>
    /// Сценарий: Переход в Tasks → Editor → Tasks — данные на месте.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_RoundTrip_DataPreserved()
    {
        var (_, vm) = CreateTasks();
        var count = vm.RunHistory.Count;

        vm.SelectScreenCommand.Execute("Editor");
        vm.SelectScreenCommand.Execute("Tasks");

        Assert.Equal(count, vm.RunHistory.Count);
    }

    /// <summary>
    /// Сценарий: Переход Tasks → Wizard → Cancel → Editor (не Tasks).
    /// </summary>
    [AvaloniaFact]
    public void Navigation_Tasks_To_Wizard_Cancel()
    {
        var (_, vm) = CreateTasks();

        vm.SelectScreenCommand.Execute("Wizard");
        vm.WizardCancelCommand.Execute(null);

        Assert.Equal(AppScreen.Editor, vm.Screen); // Cancel → Editor
    }

    // ═════════════════════════════════════════════════════════
    //  4. UI / ЛОКАЛИЗАЦИЯ
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: IsTasksscreen = true.
    /// </summary>
    [AvaloniaFact]
    public void UI_IsTasks_True()
    {
        var (_, vm) = CreateTasks();

        Assert.True(vm.IsTasks);
        Assert.False(vm.IsEditor);
        Assert.False(vm.IsSimulate);
        Assert.False(vm.IsResults);
    }

    /// <summary>
    /// Сценарий: Note для каждого статуса — на русском.
    /// </summary>
    [AvaloniaFact]
    public void Localization_Notes_Russian()
    {
        var done = new RunRecord { Status = RunStatus.Done, Duration = "01:00:00" };
        Assert.Matches(@"[а-яА-ЯёЁ]", done.Note);

        var failed = new RunRecord { Status = RunStatus.Failed };
        Assert.Matches(@"[а-яА-ЯёЁ]", failed.Note);

        var cancelled = new RunRecord { Status = RunStatus.Cancelled };
        Assert.Matches(@"[а-яА-ЯёЁ]", cancelled.Note);

        var running = new RunRecord { Status = RunStatus.Running };
        Assert.Matches(@"[а-яА-ЯёЁ]", running.Note);
    }

    /// <summary>
    /// Сценарий: Конкретные записи из SampleData — t-017, t-014, t-011.
    /// </summary>
    [AvaloniaFact]
    public void Localization_SampleRecords()
    {
        var (_, vm) = CreateTasks();

        var t017 = vm.RunHistory.First(r => r.Id == "t-017");
        Assert.Equal("MultiChannelQueue", t017.ProblemName);
        Assert.Equal(RunStatus.Done, t017.Status);
        Assert.Equal(100000, t017.Iterations);

        var t014 = vm.RunHistory.First(r => r.Id == "t-014");
        Assert.Equal(RunStatus.Failed, t014.Status);
        Assert.Equal(0, t014.Iterations);

        var t011 = vm.RunHistory.First(r => r.Id == "t-011");
        Assert.Equal(RunStatus.Cancelled, t011.Status);
        Assert.Equal(0, t011.Iterations);
    }

    // ═════════════════════════════════════════════════════════
    //  5. НЕГАТИВНЫЕ СЦЕНАРИИ
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: История пуста — RunHistory.Count == 0.
    /// </summary>
    [AvaloniaFact]
    public void Negative_EmptyHistory()
    {
        var (_, vm) = CreateTasks();

        vm.RunHistory.Clear();

        Assert.Empty(vm.RunHistory);
    }

    /// <summary>
    /// Сценарий: Очистка и добавление записей — целостность коллекции.
    /// </summary>
    [AvaloniaFact]
    public void Negative_ClearAndReAdd()
    {
        var (_, vm) = CreateTasks();

        vm.RunHistory.Clear();
        Assert.Empty(vm.RunHistory);

        vm.RunHistory.Add(new RunRecord
        {
            Id = "t-100", ProblemId = "p-001", ProblemName = "Test",
            Status = RunStatus.Done, StartedAt = "2026-05-07 12:00",
            Duration = "00:01:00", Iterations = 1000
        });

        Assert.Single(vm.RunHistory);
        Assert.Equal("t-100", vm.RunHistory[0].Id);
    }

    /// <summary>
    /// Сценарий: API offline — данные не обновляются (используются локальные).
    /// </summary>
    [AvaloniaFact]
    public void Negative_ApiOffline_LocalData()
    {
        var (_, vm) = CreateTasks();

        Assert.False(vm.IsApiOnline);
        Assert.Equal(7, vm.RunHistory.Count); // SampleData
    }

    /// <summary>
    /// Сценарий: EqualsParameterConverter — для привязки IsVisible.
    /// </summary>
    [AvaloniaFact]
    public void Negative_EqualsConverter()
    {
        var conv = EqualsParameterConverter.Instance;
        var ci = CultureInfo.InvariantCulture;

        Assert.Equal(true, conv.Convert(AppScreen.Tasks, typeof(bool), "Tasks", ci));
        Assert.Equal(false, conv.Convert(AppScreen.Tasks, typeof(bool), "Editor", ci));
        Assert.Equal(false, conv.Convert(null, typeof(bool), "Tasks", ci));
    }

    /// <summary>
    /// Сценарий: ProgressToWidthConverter — граничные значения.
    /// </summary>
    [AvaloniaFact]
    public void Negative_ProgressToWidth()
    {
        var conv = ProgressToWidthConverter.Instance;
        var ci = CultureInfo.InvariantCulture;

        // 0% → 0px
        Assert.Equal(0.0, conv.Convert(0.0, typeof(double), "580", ci));
        // 100% → 580px
        Assert.Equal(580.0, conv.Convert(1.0, typeof(double), "580", ci));
        // 50% → 290px
        Assert.Equal(290.0, conv.Convert(0.5, typeof(double), "580", ci));
        // >1 clamped → 580
        Assert.Equal(580.0, conv.Convert(1.5, typeof(double), "580", ci));
        // <0 clamped → 0
        Assert.Equal(0.0, conv.Convert(-0.5, typeof(double), "580", ci));
    }

    /// <summary>
    /// Сценарий: BoolToOpacityConverter.
    /// </summary>
    [AvaloniaFact]
    public void Negative_BoolToOpacity()
    {
        var conv = BoolToOpacityConverter.Instance;
        var ci = CultureInfo.InvariantCulture;

        Assert.Equal(1.0, conv.Convert(true, typeof(double), null, ci));
        Assert.Equal(0.0, conv.Convert(false, typeof(double), null, ci));
    }

    /// <summary>
    /// Сценарий: RunRecord со всеми пустыми полями — не крашится.
    /// </summary>
    [AvaloniaFact]
    public void Negative_EmptyRunRecord()
    {
        var r = new RunRecord();

        Assert.Equal("", r.Id);
        Assert.Equal("", r.ProblemId);
        Assert.Equal("", r.ProblemName);
        Assert.Equal("", r.StartedAt);
        Assert.Equal("", r.Duration);
        Assert.Equal(0, r.Iterations);
    }

    /// <summary>
    /// Сценарий: Множественное переключение на Tasks — данные стабильны.
    /// </summary>
    [AvaloniaFact]
    public void Negative_MultipleSwitch_Stable()
    {
        var (_, vm) = CreateTasks();

        for (int i = 0; i < 10; i++)
        {
            vm.SelectScreenCommand.Execute("Editor");
            vm.SelectScreenCommand.Execute("Tasks");
        }

        Assert.Equal(7, vm.RunHistory.Count);
        Assert.Equal(AppScreen.Tasks, vm.Screen);
    }
}
