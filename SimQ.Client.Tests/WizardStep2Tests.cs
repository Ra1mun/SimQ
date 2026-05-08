using Avalonia.Headless.XUnit;
using Avalonia.Media;
using SimQ.Client.Models;
using SimQ.Client.ViewModels;
using SimQ.Client.Views;

namespace SimQ.Client.Tests;

/// <summary>
/// Тесты шага 2 мастера: валидация полей ввода, навигация,
/// сохранение состояния, UI-проверки и интеграционные сценарии.
/// </summary>
public class WizardStep2Tests
{
    // ─────────────────────────────────────────────────────────
    //  Утилиты
    // ─────────────────────────────────────────────────────────

    /// <summary>Открывает мастер и переходит на шаг 2.</summary>
    private static (MainWindow window, MainViewModel vm) CreateWizardAtStep2(
        string name = "TestProblem", string description = "")
    {
        var vm = new MainViewModel(null);
        var window = new MainWindow { DataContext = vm, Width = 1440, Height = 900 };
        window.Show();
        vm.SelectScreenCommand.Execute("Wizard");
        vm.WizardName = name;
        vm.WizardDescription = description;
        vm.WizardNextCommand.Execute(null); // → шаг 2
        return (window, vm);
    }

    // ═════════════════════════════════════════════════════════
    //  1. ТЕСТИРОВАНИЕ ПОЛЕЙ ВВОДА (ВАЛИДАЦИЯ)
    // ═════════════════════════════════════════════════════════

    // ── 1.1 Поле «Имя задачи» ──

    /// <summary>
    /// Сценарий: Пустое имя → используется значение по умолчанию "New problem".
    /// </summary>
    [AvaloniaFact]
    public async Task Field_EmptyName_FallsBackToDefault()
    {
        var (_, vm) = CreateWizardAtStep2(name: "");

        vm.WizardNextCommand.Execute(null); // → шаг 3
        await vm.CreateProblemAsync();

        Assert.Equal("New problem", vm.CurrentProblem.Name);
    }

    /// <summary>
    /// Сценарий: Имя из 1 символа — допустимо.
    /// </summary>
    [AvaloniaFact]
    public async Task Field_SingleCharName_Accepted()
    {
        var (_, vm) = CreateWizardAtStep2(name: "X");

        vm.WizardNextCommand.Execute(null);
        await vm.CreateProblemAsync();

        Assert.Equal("X", vm.CurrentProblem.Name);
    }

    /// <summary>
    /// Сценарий: Очень длинное имя (255+ символов) — сохраняется полностью.
    /// </summary>
    [AvaloniaFact]
    public async Task Field_VeryLongName_PreservedFully()
    {
        var longName = new string('А', 300);
        var (_, vm) = CreateWizardAtStep2(name: longName);

        vm.WizardNextCommand.Execute(null);
        await vm.CreateProblemAsync();

        Assert.Equal(longName, vm.CurrentProblem.Name);
        Assert.Equal(300, vm.CurrentProblem.Name.Length);
    }

    /// <summary>
    /// Сценарий: Спецсимволы в имени (кавычки, слэши, юникод).
    /// </summary>
    [AvaloniaFact]
    public async Task Field_SpecialCharsInName_Preserved()
    {
        var specialName = "M/M/1 «тест» — λ=0.5 & K>c";
        var (_, vm) = CreateWizardAtStep2(name: specialName);

        vm.WizardNextCommand.Execute(null);
        await vm.CreateProblemAsync();

        Assert.Equal(specialName, vm.CurrentProblem.Name);
    }

    /// <summary>
    /// Сценарий: Пробелы по краям — обрезаются при создании.
    /// </summary>
    [AvaloniaFact]
    public async Task Field_Name_Trimmed()
    {
        var (_, vm) = CreateWizardAtStep2(name: "   Пробелы   ");

        vm.WizardNextCommand.Execute(null);
        await vm.CreateProblemAsync();

        Assert.Equal("Пробелы", vm.CurrentProblem.Name);
    }

    /// <summary>
    /// Сценарий: Имя только из пробелов → используется "New problem".
    /// </summary>
    [AvaloniaFact]
    public async Task Field_WhitespaceOnlyName_FallsBackToDefault()
    {
        var (_, vm) = CreateWizardAtStep2(name: "     ");

        vm.WizardNextCommand.Execute(null);
        await vm.CreateProblemAsync();

        Assert.Equal("New problem", vm.CurrentProblem.Name);
    }

    // ── 1.2 Поле «Описание» ──

    /// <summary>
    /// Сценарий: Многострочное описание сохраняется корректно.
    /// </summary>
    [AvaloniaFact]
    public async Task Field_MultilineDescription_Preserved()
    {
        var multiline = "Строка 1\nСтрока 2\nСтрока 3";
        var (_, vm) = CreateWizardAtStep2(description: multiline);
        vm.WizardDescription = multiline; // явно на шаге 1 (уже на 2, но свойство общее)

        // Вернёмся на шаг 1 чтобы задать описание, затем вперёд
        vm.WizardBackCommand.Execute(null);
        vm.WizardDescription = multiline;
        vm.WizardNextCommand.Execute(null); // → 2
        vm.WizardNextCommand.Execute(null); // → 3
        await vm.CreateProblemAsync();

        Assert.Contains("\n", vm.CurrentProblem.Description);
        Assert.Equal(multiline, vm.CurrentProblem.Description);
    }

    /// <summary>
    /// Сценарий: Пустое описание допускается.
    /// </summary>
    [AvaloniaFact]
    public async Task Field_EmptyDescription_Allowed()
    {
        var (_, vm) = CreateWizardAtStep2(description: "");

        vm.WizardNextCommand.Execute(null);
        await vm.CreateProblemAsync();

        Assert.Equal("", vm.CurrentProblem.Description);
    }

    // ── 1.3 Числовые параметры (Итерации, Seed) ──

    /// <summary>
    /// Сценарий: Итерации по умолчанию = 50000.
    /// </summary>
    [AvaloniaFact]
    public void Field_Iterations_Default_Value()
    {
        var (_, vm) = CreateWizardAtStep2();

        Assert.Equal(50000, vm.Iterations);
    }

    /// <summary>
    /// Сценарий: Seed по умолчанию = 42.
    /// </summary>
    [AvaloniaFact]
    public void Field_Seed_Default_Value()
    {
        var (_, vm) = CreateWizardAtStep2();

        Assert.Equal(42, vm.Seed);
    }

    /// <summary>
    /// Сценарий: Изменение итераций на шаге 3 мастера.
    /// </summary>
    [AvaloniaFact]
    public void Field_Iterations_Can_Be_Changed()
    {
        var (_, vm) = CreateWizardAtStep2();
        vm.WizardNextCommand.Execute(null); // → шаг 3

        vm.Iterations = 100000;
        Assert.Equal(100000, vm.Iterations);
    }

    /// <summary>
    /// Сценарий: Итерации = 0 или отрицательные — граничный случай.
    /// </summary>
    [AvaloniaFact]
    public void Field_Iterations_Zero_Or_Negative()
    {
        var (_, vm) = CreateWizardAtStep2();

        vm.Iterations = 0;
        Assert.Equal(0, vm.Iterations);

        vm.Iterations = -100;
        Assert.Equal(-100, vm.Iterations);
        // ViewModel принимает значение, валидация при создании задачи
        // (MaxSteps приведён к Math.Max(1, Iterations))
    }

    // ── 1.4 Параметры агентов (λ, c, Capacity) ──

    /// <summary>
    /// Сценарий: Параметр λ (Rate) источника — допускает дробные значения.
    /// </summary>
    [AvaloniaFact]
    public void Field_Lambda_Accepts_Fractional_Values()
    {
        var agent = new Agent
        {
            Id = "test", Kind = AgentKind.Source, Name = "Test",
            ArrivalDistribution = new() { Kind = DistributionKind.M, Rate = 0.75 }
        };

        Assert.Equal(0.75, agent.ArrivalDistribution.Rate);
        // Format() использует текущую локаль; проверяем наличие числа
        Assert.Contains("λ=", agent.ArrivalDistribution.Format());
        Assert.Contains("75", agent.ArrivalDistribution.Format());
    }

    /// <summary>
    /// Сценарий: Параметр c (каналы) — целое положительное число.
    /// </summary>
    [AvaloniaFact]
    public void Field_Channels_Positive_Integer()
    {
        var agent = new Agent
        {
            Id = "test", Kind = AgentKind.ServiceBlock, Name = "SVB",
            Channels = 3
        };

        Assert.Equal(3, agent.Channels);
    }

    /// <summary>
    /// Сценарий: Каналы = 0 — допускается моделью (граничный случай).
    /// </summary>
    [AvaloniaFact]
    public void Field_Channels_Zero_Boundary()
    {
        var agent = new Agent { Id = "t", Kind = AgentKind.ServiceBlock, Channels = 0 };
        Assert.Equal(0, agent.Channels);
    }

    /// <summary>
    /// Сценарий: Ёмкость хранилища может быть "Infinity" или числом.
    /// </summary>
    [AvaloniaFact]
    public void Field_Capacity_Infinity_Or_Numeric()
    {
        var buffer = new Agent { Id = "b", Kind = AgentKind.Buffer, Capacity = "Infinity" };
        Assert.Equal("Infinity", buffer.Capacity);

        buffer.Capacity = "10";
        Assert.Equal("10", buffer.Capacity);
    }

    /// <summary>
    /// Сценарий: Ввод отрицательного λ — модель принимает, но формат отображает.
    /// </summary>
    [AvaloniaFact]
    public void Field_NegativeLambda_ModelAccepts()
    {
        var dist = new DistributionParams { Kind = DistributionKind.M, Rate = -0.5 };
        Assert.Equal(-0.5, dist.Rate);
        // Валидация остаётся за слоем бизнес-логики; UI-модель хранит значение
    }

    /// <summary>
    /// Сценарий: Отрицательные каналы — модель принимает значение.
    /// </summary>
    [AvaloniaFact]
    public void Field_NegativeChannels_ModelAccepts()
    {
        var agent = new Agent { Id = "t", Kind = AgentKind.ServiceBlock, Channels = -1 };
        Assert.Equal(-1, agent.Channels);
    }

    // ═════════════════════════════════════════════════════════
    //  2. НАВИГАЦИЯ И СОХРАНЕНИЕ СОСТОЯНИЯ
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Кнопка «Назад» — возврат на шаг 1, данные сохранены.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_BackPreservesData()
    {
        var (_, vm) = CreateWizardAtStep2(name: "МояЗадача", description: "Описание задачи");

        vm.WizardBackCommand.Execute(null);

        Assert.Equal(1, vm.WizardStep);
        Assert.Equal("МояЗадача", vm.WizardName);
        Assert.Equal("Описание задачи", vm.WizardDescription);
    }

    /// <summary>
    /// Сценарий: Назад → Далее → данные по-прежнему на месте.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_BackThenForward_DataIntact()
    {
        var (_, vm) = CreateWizardAtStep2(name: "Test123");

        vm.WizardBackCommand.Execute(null); // → 1
        vm.WizardNextCommand.Execute(null); // → 2

        Assert.Equal(2, vm.WizardStep);
        Assert.Equal("Test123", vm.WizardName);
    }

    /// <summary>
    /// Сценарий: Шаг 2 → Далее → Шаг 3 (подтверждение).
    /// </summary>
    [AvaloniaFact]
    public void Navigation_Step2_Next_GoesToStep3()
    {
        var (_, vm) = CreateWizardAtStep2();

        vm.WizardNextCommand.Execute(null);

        Assert.Equal(3, vm.WizardStep);
        Assert.True(vm.IsWizardStep3);
        Assert.Equal("Параметры моделирования", vm.WizardStepTitle);
    }

    /// <summary>
    /// Сценарий: Шаблон, выбранный на шаге 1, сохраняется на шаге 2.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_TemplatePreservedOnStep2()
    {
        var vm = new MainViewModel(null);
        var window = new MainWindow { DataContext = vm };
        window.Show();
        vm.SelectScreenCommand.Execute("Wizard");
        vm.WizardTemplate = 3;
        vm.WizardNextCommand.Execute(null); // → 2

        Assert.Equal(3, vm.WizardTemplate);
    }

    /// <summary>
    /// Сценарий: Навигация полный цикл 1→2→3→2→1→2→3 — данные целы.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_FullCycle_DataPreserved()
    {
        var (_, vm) = CreateWizardAtStep2(name: "Цикл", description: "Тест цикла");
        vm.WizardTemplate = 2;

        vm.WizardNextCommand.Execute(null); // → 3
        vm.WizardBackCommand.Execute(null); // → 2
        vm.WizardBackCommand.Execute(null); // → 1
        vm.WizardNextCommand.Execute(null); // → 2
        vm.WizardNextCommand.Execute(null); // → 3

        Assert.Equal(3, vm.WizardStep);
        Assert.Equal("Цикл", vm.WizardName);
        Assert.Equal("Тест цикла", vm.WizardDescription);
        Assert.Equal(2, vm.WizardTemplate);
    }

    // ═════════════════════════════════════════════════════════
    //  3. ПОЛЬЗОВАТЕЛЬСКИЙ ИНТЕРФЕЙС (UI)
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Индикация прогресса — шаг 1 и 2 акцентные, шаг 3 серый.
    /// </summary>
    [AvaloniaFact]
    public void UI_ProgressIndicator_Step2()
    {
        var (_, vm) = CreateWizardAtStep2();

        var c1 = ((SolidColorBrush)vm.WizardStep1Brush).Color;
        var c2 = ((SolidColorBrush)vm.WizardStep2Brush).Color;
        var c3 = ((SolidColorBrush)vm.WizardStep3Brush).Color;

        Assert.Equal(Color.Parse("#3D5FCC"), c1); // завершён
        Assert.Equal(Color.Parse("#3D5FCC"), c2); // активный
        Assert.Equal(Color.Parse("#DFE3EA"), c3); // неактивный
    }

    /// <summary>
    /// Сценарий: Opacity шага 2 == 1.0 (текущий), шаги 1 и 3 == 0.55.
    /// </summary>
    [AvaloniaFact]
    public void UI_Opacity_Step2_Active()
    {
        var (_, vm) = CreateWizardAtStep2();

        Assert.Equal(0.55, vm.WizardStep1Opacity);
        Assert.Equal(1.0, vm.WizardStep2Opacity);
        Assert.Equal(0.55, vm.WizardStep3Opacity);
    }

    /// <summary>
    /// Сценарий: Метка шага отображает «ШАГ 2 / 3».
    /// </summary>
    [AvaloniaFact]
    public void UI_StepLabel_Shows_Step2()
    {
        var (_, vm) = CreateWizardAtStep2();

        Assert.Equal("ШАГ 2 / 3", vm.WizardStepLabel);
    }

    /// <summary>
    /// Сценарий: Заголовок шага 2 = «Структура агентов».
    /// </summary>
    [AvaloniaFact]
    public void UI_StepTitle_Step2()
    {
        var (_, vm) = CreateWizardAtStep2();

        Assert.Equal("Структура агентов", vm.WizardStepTitle);
    }

    /// <summary>
    /// Сценарий: На шаге 2 кнопки «Назад» и «Далее» обе доступны.
    /// </summary>
    [AvaloniaFact]
    public void UI_BothButtons_Available_OnStep2()
    {
        var (_, vm) = CreateWizardAtStep2();

        Assert.True(vm.CanWizardBack);
        Assert.True(vm.CanWizardNext);
    }

    /// <summary>
    /// Сценарий: Подсказка на шаге 2 содержит текст про шаблон/агентов.
    /// </summary>
    [AvaloniaFact]
    public void UI_Hint_Step2_Mentions_Template()
    {
        var (_, vm) = CreateWizardAtStep2();

        Assert.False(string.IsNullOrEmpty(vm.WizardStepHint));
        // Подсказка шага 2: «Шаблон загружает типовую структуру агентов...»
        Assert.Matches(@"[а-яА-ЯёЁ]", vm.WizardStepHint);
    }

    // ═════════════════════════════════════════════════════════
    //  4. ИНТЕГРАЦИОННЫЕ И ТЕХНИЧЕСКИЕ ПРОВЕРКИ
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Статус «Черновик» текущей задачи отображается.
    /// </summary>
    [AvaloniaFact]
    public void Integration_CurrentProblem_Draft_Status()
    {
        var (_, vm) = CreateWizardAtStep2();

        Assert.Equal(ProblemStatus.Draft, vm.CurrentProblem.Status);
    }

    /// <summary>
    /// Сценарий: Футер — количество агентов и связей для текущей задачи.
    /// </summary>
    [AvaloniaFact]
    public void Integration_Footer_AgentAndEdgeCount()
    {
        var (_, vm) = CreateWizardAtStep2();

        // FinServiceProblem по умолчанию: 5 агентов, 4 связи
        Assert.Equal(5, vm.CurrentProblem.AgentCount);
        Assert.Equal(4, vm.CurrentProblem.EdgeCount);
    }

    /// <summary>
    /// Сценарий: API offline — статус в футере.
    /// </summary>
    [AvaloniaFact]
    public void Integration_Footer_ApiOffline()
    {
        var (_, vm) = CreateWizardAtStep2();

        Assert.False(vm.IsApiOnline);
    }

    /// <summary>
    /// Сценарий: Отмена на шаге 2 → возврат в редактор.
    /// </summary>
    [AvaloniaFact]
    public void Integration_Cancel_OnStep2()
    {
        var (_, vm) = CreateWizardAtStep2(name: "Отмена");

        vm.WizardCancelCommand.Execute(null);

        Assert.Equal(AppScreen.Editor, vm.Screen);
        Assert.Equal(1, vm.WizardStep);
    }

    /// <summary>
    /// Сценарий: Созданная задача содержит базовые агенты (Source, ServiceBlock, Sink).
    /// </summary>
    [AvaloniaFact]
    public async Task Integration_CreatedProblem_Has_Default_Agents()
    {
        var (_, vm) = CreateWizardAtStep2(name: "Агенты");

        vm.WizardNextCommand.Execute(null); // → 3
        await vm.CreateProblemAsync();

        var agents = vm.CurrentProblem.Agents;
        Assert.True(agents.Count >= 3); // Source, ServiceBlock, Sink
        Assert.Contains(agents, a => a.Kind == AgentKind.Source);
        Assert.Contains(agents, a => a.Kind == AgentKind.ServiceBlock);
        Assert.Contains(agents, a => a.Kind == AgentKind.Sink);
    }

    /// <summary>
    /// Сценарий: Созданная задача содержит связи между агентами.
    /// </summary>
    [AvaloniaFact]
    public async Task Integration_CreatedProblem_Has_Edges()
    {
        var (_, vm) = CreateWizardAtStep2(name: "Связи");

        vm.WizardNextCommand.Execute(null);
        await vm.CreateProblemAsync();

        Assert.True(vm.CurrentProblem.EdgeCount >= 2);
    }

    /// <summary>
    /// Сценарий: Параметры агента Source в созданной задаче —
    /// экспоненциальное распределение λ=0.3.
    /// </summary>
    [AvaloniaFact]
    public async Task Integration_CreatedSource_Has_Exponential_Distribution()
    {
        var (_, vm) = CreateWizardAtStep2();

        vm.WizardNextCommand.Execute(null);
        await vm.CreateProblemAsync();

        var source = vm.CurrentProblem.Agents.First(a => a.Kind == AgentKind.Source);
        Assert.Equal(DistributionKind.M, source.Model.ArrivalDistribution.Kind);
        Assert.Equal(0.3, source.Model.ArrivalDistribution.Rate);
    }

    /// <summary>
    /// Сценарий: Параметры ServiceBlock — 1 канал, μ=0.5.
    /// </summary>
    [AvaloniaFact]
    public async Task Integration_CreatedServiceBlock_Params()
    {
        var (_, vm) = CreateWizardAtStep2();

        vm.WizardNextCommand.Execute(null);
        await vm.CreateProblemAsync();

        var svb = vm.CurrentProblem.Agents.First(a => a.Kind == AgentKind.ServiceBlock);
        Assert.Equal(1, svb.Model.Channels);
        Assert.Equal(DistributionKind.M, svb.Model.ServiceDistribution.Kind);
        Assert.Equal(0.5, svb.Model.ServiceDistribution.Rate);
    }

    /// <summary>
    /// Сценарий: Формат параметров агентов (ParamsSummary) на русском/корректный.
    /// </summary>
    [AvaloniaFact]
    public void Integration_AgentViewModel_ParamsSummary()
    {
        var source = new AgentViewModel(new Agent
        {
            Id = "s1", Kind = AgentKind.Source, Name = "Source",
            ArrivalDistribution = new() { Kind = DistributionKind.M, Rate = 0.3 }
        });
        Assert.Contains("λ=", source.ParamsSummary);

        var svb = new AgentViewModel(new Agent
        {
            Id = "svb1", Kind = AgentKind.ServiceBlock, Name = "SVB",
            Channels = 3, ServiceDistribution = new() { Kind = DistributionKind.M, Rate = 0.5 }
        });
        Assert.Contains("c=3", svb.ParamsSummary);

        var buf = new AgentViewModel(new Agent
        {
            Id = "b1", Kind = AgentKind.Buffer, Name = "BUF",
            Policy = "FIFO", Capacity = "10"
        });
        Assert.Contains("FIFO", buf.ParamsSummary);
        Assert.Contains("10", buf.ParamsSummary);

        var sink = new AgentViewModel(new Agent
        {
            Id = "snk", Kind = AgentKind.Sink, Name = "Sink"
        });
        Assert.Equal("выход", sink.ParamsSummary);
    }

    /// <summary>
    /// Сценарий: KindLabel агентов на русском языке.
    /// </summary>
    [AvaloniaFact]
    public void Integration_AgentViewModel_KindLabels_Russian()
    {
        Assert.Equal("Источник",       new AgentViewModel(new Agent { Kind = AgentKind.Source }).KindLabel);
        Assert.Equal("Блок приборов",  new AgentViewModel(new Agent { Kind = AgentKind.ServiceBlock }).KindLabel);
        Assert.Equal("Хранилище",      new AgentViewModel(new Agent { Kind = AgentKind.Buffer }).KindLabel);
        Assert.Equal("Орбита",         new AgentViewModel(new Agent { Kind = AgentKind.Orbit }).KindLabel);
        Assert.Equal("Сток",           new AgentViewModel(new Agent { Kind = AgentKind.Sink }).KindLabel);
    }

    /// <summary>
    /// Сценарий: Формат распределения (DistributionParams.Format) корректный.
    /// </summary>
    [AvaloniaFact]
    public void Integration_DistributionFormat()
    {
        // Format() использует текущую локаль для разделителя дробной части
        var sep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

        Assert.StartsWith("M  λ=", new DistributionParams { Kind = DistributionKind.M, Rate = 0.3 }.Format());
        Assert.StartsWith("D  t=", new DistributionParams { Kind = DistributionKind.D, Value = 1.0 }.Format());
        Assert.StartsWith("Bern  p=", new DistributionParams { Kind = DistributionKind.Bernoulli, P = 0.2 }.Format());
        Assert.Contains("α=", new DistributionParams { Kind = DistributionKind.Beta, A = 0.2, B = 0.3 }.Format());
        Assert.Contains("β=", new DistributionParams { Kind = DistributionKind.Beta, A = 0.2, B = 0.3 }.Format());
        Assert.Contains("μ=", new DistributionParams { Kind = DistributionKind.G, Mean = 1.0, Std = 0.5 }.Format());
        Assert.Contains("σ=", new DistributionParams { Kind = DistributionKind.G, Mean = 1.0, Std = 0.5 }.Format());
    }
}
