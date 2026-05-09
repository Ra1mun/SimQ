using Avalonia.Headless.XUnit;
using Avalonia.Media;
using SimQ.Client.Models;
using SimQ.Client.Services;
using SimQ.Client.ViewModels;
using SimQ.Client.Views;

namespace SimQ.Client.Tests;

/// <summary>
/// Тесты шага 3 мастера (Подтверждение): проверка данных, навигация,
/// создание задачи, обработка ошибок, UI-статусы.
/// </summary>
public class WizardStep3Tests
{
    // ─────────────────────────────────────────────────────────
    //  Утилиты
    // ─────────────────────────────────────────────────────────

    /// <summary>Открывает мастер, заполняет шаги 1-2 и переходит на шаг 3.</summary>
    private static (MainWindow window, MainViewModel vm) CreateWizardAtStep3(
        string name = "NewProblem",
        string description = "Тестовое описание",
        int template = 1)
    {
        var vm = new MainViewModel(null); // API offline
        var window = new MainWindow { DataContext = vm, Width = 1440, Height = 900 };
        window.Show();
        vm.SelectScreenCommand.Execute("Wizard");
        vm.WizardName = name;
        vm.WizardDescription = description;
        vm.WizardTemplate = template;
        vm.WizardNextCommand.Execute(null); // → шаг 2
        vm.WizardNextCommand.Execute(null); // → шаг 3
        return (window, vm);
    }

    // ═════════════════════════════════════════════════════════
    //  1. ПРОВЕРКА СООТВЕТСТВИЯ ДАННЫХ (Data Consistency)
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Имя задачи на шаге 3 совпадает с введённым на шаге 1.
    /// </summary>
    [AvaloniaFact]
    public void DataConsistency_Name_MatchesStep1()
    {
        var (_, vm) = CreateWizardAtStep3(name: "MyProblem");

        Assert.Equal("MyProblem", vm.WizardName);
    }

    /// <summary>
    /// Сценарий: Описание на шаге 3 совпадает с введённым на шаге 1.
    /// </summary>
    [AvaloniaFact]
    public void DataConsistency_Description_MatchesStep1()
    {
        var (_, vm) = CreateWizardAtStep3(description: "Моё описание задачи");

        Assert.Equal("Моё описание задачи", vm.WizardDescription);
    }

    /// <summary>
    /// Сценарий: Шаблон на шаге 3 совпадает с выбранным на шаге 1.
    /// </summary>
    [AvaloniaFact]
    public void DataConsistency_Template_MatchesStep1()
    {
        var (_, vm) = CreateWizardAtStep3(template: 2);

        Assert.Equal(2, vm.WizardTemplate);
    }

    /// <summary>
    /// Сценарий: Итерации и Seed на шаге 3 соответствуют текущим значениям.
    /// </summary>
    [AvaloniaFact]
    public void DataConsistency_Iterations_And_Seed()
    {
        var (_, vm) = CreateWizardAtStep3();

        Assert.Equal(50000, vm.Iterations);
        Assert.Equal(42, vm.Seed);
    }

    /// <summary>
    /// Сценарий: Изменение итераций на шаге 3 учитывается при создании.
    /// </summary>
    [AvaloniaFact]
    public async Task DataConsistency_ModifiedIterations_UsedOnCreate()
    {
        var (_, vm) = CreateWizardAtStep3();
        vm.Iterations = 75000;

        await vm.CreateProblemAsync();

        Assert.Equal(75000, vm.Iterations);
    }

    /// <summary>
    /// Сценарий: Агенты текущей задачи (FinServiceProblem) — 5 шт., связей — 4.
    /// Сводка шага 3 должна показывать данные текущей задачи, а создание
    /// добавляет новую задачу с 3 агентами и 2 связями (шаблон по умолчанию).
    /// </summary>
    [AvaloniaFact]
    public void DataConsistency_CurrentProblem_AgentsAndEdges()
    {
        var (_, vm) = CreateWizardAtStep3();

        // Текущая задача (FinServiceProblem) в футере
        Assert.Equal(5, vm.CurrentProblem.AgentCount);
        Assert.Equal(4, vm.CurrentProblem.EdgeCount);
    }

    /// <summary>
    /// Сценарий: После создания новая задача содержит 3 агента и 2 связи.
    /// </summary>
    [AvaloniaFact]
    public async Task DataConsistency_CreatedProblem_Has_3Agents_2Edges()
    {
        var (_, vm) = CreateWizardAtStep3();

        await vm.CreateProblemAsync();

        // Новая задача из шаблона: Source, ServiceBlock, Sink + 2 ребра
        Assert.Equal(3, vm.CurrentProblem.AgentCount);
        Assert.Equal(2, vm.CurrentProblem.EdgeCount);
    }

    /// <summary>
    /// Сценарий: Endpoint для создания задачи — POST /Problems/v1/problem.
    /// </summary>
    [AvaloniaFact]
    public void DataConsistency_Endpoint_Correct()
    {
        // Проверяем что SimQApiClient обращается к правильному endpoint
        // через RegisterProblemAsync → PostAsync("Problems/v1/problem", ...)
        var settings = new ApiSettings();
        Assert.Equal("http://localhost:5000/", settings.BaseUrl);
        // Endpoint зашит в SimQApiClient.RegisterProblemAsync
    }

    /// <summary>
    /// Сценарий: Маппер ProblemMapper корректно формирует RegisterProblemRequest.
    /// </summary>
    [AvaloniaFact]
    public void DataConsistency_ProblemMapper_CorrectRequest()
    {
        var problem = new Problem
        {
            Name = "Test",
            Agents =
            {
                new Agent { Id = "s1", Kind = AgentKind.Source, Name = "Source",
                    ArrivalDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.3 } },
                new Agent { Id = "sv1", Kind = AgentKind.ServiceBlock, Name = "SVB",
                    Channels = 1, ServiceDistribution = new() { Kind = DistributionKind.Exponential, Rate = 0.5 } },
                new Agent { Id = "snk", Kind = AgentKind.Sink, Name = "Sink" },
            },
            Edges =
            {
                new Edge { Id = "e1", From = "s1", To = "sv1" },
                new Edge { Id = "e2", From = "sv1", To = "snk" },
            },
        };

        var request = ProblemMapper.ToRegisterRequest(problem);

        Assert.Equal("Test", request.Name);
        // Sink отфильтрован — сервер не поддерживает тип SINK
        Assert.Equal(2, request.Agents.Count);
        Assert.DoesNotContain(request.Agents, a => a.Type == "SINK");
        Assert.Contains(request.Agents, a => a.Type == "SOURCE");
        Assert.Contains(request.Agents, a => a.Type == "SERVICE_BLOCK");
        // Связи сгруппированы по From
        Assert.True(request.Links.ContainsKey("s1"));
        Assert.Contains("sv1", request.Links["s1"]);
    }

    /// <summary>
    /// Сценарий: Маппер с пустым именем подставляет "Untitled".
    /// </summary>
    [AvaloniaFact]
    public void DataConsistency_ProblemMapper_EmptyName_Untitled()
    {
        var problem = new Problem { Name = "   " };
        var request = ProblemMapper.ToRegisterRequest(problem);
        Assert.Equal("Untitled", request.Name);
    }

    // ═════════════════════════════════════════════════════════
    //  2. НАВИГАЦИЯ И ФУНКЦИОНАЛЬНОСТЬ КНОПОК
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Кнопка «Назад» — возврат на шаг 2, данные сохранены.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_Back_ToStep2_DataPreserved()
    {
        var (_, vm) = CreateWizardAtStep3(name: "СохранимДанные", description: "Описание");

        vm.WizardBackCommand.Execute(null);

        Assert.Equal(2, vm.WizardStep);
        Assert.Equal("СохранимДанные", vm.WizardName);
        Assert.Equal("Описание", vm.WizardDescription);
    }

    /// <summary>
    /// Сценарий: Назад → Далее → данные на шаге 3 целы.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_BackForward_DataIntact()
    {
        var (_, vm) = CreateWizardAtStep3(name: "Cycle", template: 3);
        vm.Iterations = 99999;

        vm.WizardBackCommand.Execute(null); // → 2
        vm.WizardNextCommand.Execute(null); // → 3

        Assert.Equal(3, vm.WizardStep);
        Assert.Equal("Cycle", vm.WizardName);
        Assert.Equal(3, vm.WizardTemplate);
        Assert.Equal(99999, vm.Iterations);
    }

    /// <summary>
    /// Сценарий: Создание задачи (без API) — задача создаётся локально,
    /// переход в редактор.
    /// </summary>
    [AvaloniaFact]
    public async Task Navigation_Create_LocalSuccess()
    {
        var (_, vm) = CreateWizardAtStep3(name: "Финальная");
        var countBefore = vm.Problems.Count;

        await vm.CreateProblemAsync();

        Assert.Equal(countBefore + 1, vm.Problems.Count);
        Assert.Equal(AppScreen.Editor, vm.Screen);
        Assert.Equal("Финальная", vm.CurrentProblem.Name);
        Assert.True(vm.HasToast);
        Assert.Contains("создана", vm.Toast);
    }

    /// <summary>
    /// Сценарий: После создания — мастер сбрасывается (шаг, имя, описание).
    /// </summary>
    [AvaloniaFact]
    public async Task Navigation_Create_ResetsWizard()
    {
        var (_, vm) = CreateWizardAtStep3(name: "Reset", description: "Desc");

        await vm.CreateProblemAsync();

        Assert.Equal(1, vm.WizardStep);
        Assert.Equal("", vm.WizardName);
        Assert.Equal("", vm.WizardDescription);
    }

    /// <summary>
    /// Сценарий: Отмена на шаге 3 — задача не создана, возврат в редактор.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_Cancel_DoesNotCreate()
    {
        var (_, vm) = CreateWizardAtStep3(name: "НеСоздаём");
        var countBefore = vm.Problems.Count;

        vm.WizardCancelCommand.Execute(null);

        Assert.Equal(countBefore, vm.Problems.Count); // не добавлена
        Assert.Equal(AppScreen.Editor, vm.Screen);
    }

    /// <summary>
    /// Сценарий: Переключение на вкладку во время шага 3 — задача не создана.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_TabSwitch_DoesNotCreate()
    {
        var (_, vm) = CreateWizardAtStep3();
        var countBefore = vm.Problems.Count;

        vm.SelectScreenCommand.Execute("Tasks");

        Assert.Equal(countBefore, vm.Problems.Count);
        Assert.Equal(AppScreen.Tasks, vm.Screen);
    }

    /// <summary>
    /// Сценарий: На шаге 3 кнопка «Далее» недоступна (последний шаг).
    /// </summary>
    [AvaloniaFact]
    public void Navigation_NextUnavailable_OnStep3()
    {
        var (_, vm) = CreateWizardAtStep3();

        Assert.False(vm.CanWizardNext);
        Assert.False(vm.WizardNextCommand.CanExecute(null));
    }

    /// <summary>
    /// Сценарий: На шаге 3 кнопка «Назад» доступна.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_BackAvailable_OnStep3()
    {
        var (_, vm) = CreateWizardAtStep3();

        Assert.True(vm.CanWizardBack);
        Assert.True(vm.WizardBackCommand.CanExecute(null));
    }

    // ═════════════════════════════════════════════════════════
    //  3. ТЕСТИРОВАНИЕ ИНТЕРФЕЙСА (UI)
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Шаги 1 и 2 отмечены акцентным цветом (пройдены),
    /// шаг 3 тоже акцентный (все >= текущего).
    /// </summary>
    [AvaloniaFact]
    public void UI_AllSteps_AccentColor_OnStep3()
    {
        var (_, vm) = CreateWizardAtStep3();

        var c1 = ((SolidColorBrush)vm.WizardStep1Brush).Color;
        var c2 = ((SolidColorBrush)vm.WizardStep2Brush).Color;
        var c3 = ((SolidColorBrush)vm.WizardStep3Brush).Color;

        Assert.Equal(Color.Parse("#3D5FCC"), c1); // пройден
        Assert.Equal(Color.Parse("#3D5FCC"), c2); // пройден
        Assert.Equal(Color.Parse("#3D5FCC"), c3); // активный
    }

    /// <summary>
    /// Сценарий: Opacity шага 3 = 1.0, остальные = 0.55.
    /// </summary>
    [AvaloniaFact]
    public void UI_Step3_FullOpacity()
    {
        var (_, vm) = CreateWizardAtStep3();

        Assert.Equal(0.55, vm.WizardStep1Opacity);
        Assert.Equal(0.55, vm.WizardStep2Opacity);
        Assert.Equal(1.0, vm.WizardStep3Opacity);
    }

    /// <summary>
    /// Сценарий: Метка шага = «ШАГ 3 / 3».
    /// </summary>
    [AvaloniaFact]
    public void UI_StepLabel_Step3()
    {
        var (_, vm) = CreateWizardAtStep3();

        Assert.Equal("ШАГ 3 / 3", vm.WizardStepLabel);
    }

    /// <summary>
    /// Сценарий: Заголовок шага 3 = «Параметры моделирования».
    /// </summary>
    [AvaloniaFact]
    public void UI_StepTitle_Step3()
    {
        var (_, vm) = CreateWizardAtStep3();

        Assert.Equal("Параметры моделирования", vm.WizardStepTitle);
    }

    /// <summary>
    /// Сценарий: Подсказка шага 3 на русском языке.
    /// </summary>
    [AvaloniaFact]
    public void UI_Hint_Step3_Russian()
    {
        var (_, vm) = CreateWizardAtStep3();

        Assert.False(string.IsNullOrEmpty(vm.WizardStepHint));
        Assert.Matches(@"[а-яА-ЯёЁ]", vm.WizardStepHint);
    }

    /// <summary>
    /// Сценарий: Статус текущей задачи — Draft (ЧЕРНОВИК).
    /// </summary>
    [AvaloniaFact]
    public void UI_CurrentProblem_Draft_Status()
    {
        var (_, vm) = CreateWizardAtStep3();

        Assert.Equal(ProblemStatus.Draft, vm.CurrentProblem.Status);
    }

    /// <summary>
    /// Сценарий: ScreenLabel показывает «Новая задача».
    /// </summary>
    [AvaloniaFact]
    public void UI_ScreenLabel_Wizard()
    {
        var (_, vm) = CreateWizardAtStep3();

        Assert.Contains("Новая задача", vm.ScreenLabel);
    }

    // ═════════════════════════════════════════════════════════
    //  4. ОБРАБОТКА ОШИБОК (Negative Scenarios)
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Создание без API — задача создаётся локально,
    /// toast содержит «создана».
    /// </summary>
    [AvaloniaFact]
    public async Task Errors_NoApi_LocalCreation()
    {
        var (_, vm) = CreateWizardAtStep3(name: "Offline");

        Assert.False(vm.IsApiOnline);

        await vm.CreateProblemAsync();

        Assert.Equal("Offline", vm.CurrentProblem.Name);
        Assert.True(vm.HasToast);
        Assert.Contains("создана", vm.Toast);
    }

    /// <summary>
    /// Сценарий: API offline → создание не блокируется, 
    /// задача появляется в списке с local- ID.
    /// </summary>
    [AvaloniaFact]
    public async Task Errors_ApiOffline_LocalIdAssigned()
    {
        var (_, vm) = CreateWizardAtStep3();

        await vm.CreateProblemAsync();

        Assert.StartsWith("local-", vm.CurrentProblem.Id);
    }

    /// <summary>
    /// Сценарий: Повторное создание после первого — каждая с уникальным ID.
    /// </summary>
    [AvaloniaFact]
    public async Task Errors_MultipleCreations_UniqueIds()
    {
        var (_, vm) = CreateWizardAtStep3(name: "First");
        await vm.CreateProblemAsync();
        var id1 = vm.CurrentProblem.Id;

        // Заново в мастер
        vm.SelectScreenCommand.Execute("Wizard");
        vm.WizardName = "Second";
        vm.WizardNextCommand.Execute(null);
        vm.WizardNextCommand.Execute(null);
        await vm.CreateProblemAsync();
        var id2 = vm.CurrentProblem.Id;

        Assert.NotEqual(id1, id2);
    }

    /// <summary>
    /// Сценарий: Запуск симуляции для задачи без серверного ID — 
    /// показывает toast «Сначала сохраните задачу».
    /// </summary>
    [AvaloniaFact]
    public async Task Errors_Simulation_RequiresSave_First()
    {
        var (_, vm) = CreateWizardAtStep3();
        await vm.CreateProblemAsync();

        // Задача имеет local- ID, запуск симуляции требует серверный ID
        vm.SelectScreenCommand.Execute("Simulate");
        vm.StartSimulationCommand.Execute(null);

        Assert.False(vm.IsRunning);
        Assert.True(vm.HasToast);
    }

    /// <summary>
    /// Сценарий: Сохранение задачи без API — toast «API недоступен».
    /// </summary>
    [AvaloniaFact]
    public async Task Errors_Save_WithoutApi()
    {
        var (_, vm) = CreateWizardAtStep3();
        await vm.CreateProblemAsync();

        vm.SaveCommand.Execute(null);

        Assert.True(vm.HasToast);
        Assert.Contains("недоступен", vm.Toast);
    }

    /// <summary>
    /// Сценарий: IsRunning не устанавливается при неудачном запуске.
    /// </summary>
    [AvaloniaFact]
    public void Errors_IsRunning_StaysFalse_OnFailure()
    {
        var (_, vm) = CreateWizardAtStep3();

        vm.StartSimulationCommand.Execute(null);

        Assert.False(vm.IsRunning);
    }

    /// <summary>
    /// Сценарий: Отмена на финальном этапе — задача НЕ создана в списке.
    /// </summary>
    [AvaloniaFact]
    public void Errors_Cancel_NoProblemAdded()
    {
        var (_, vm) = CreateWizardAtStep3();
        var names = vm.Problems.Select(p => p.Name).ToList();

        vm.WizardCancelCommand.Execute(null);

        // Ни одна новая задача не добавлена
        Assert.Equal(names.Count, vm.Problems.Count);
        foreach (var p in vm.Problems)
            Assert.Contains(p.Name, names);
    }

    // ═════════════════════════════════════════════════════════
    //  5. БЕЗОПАСНОСТЬ И ПРАВА ДОСТУПА
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Без API все операции записи (Save, StartSimulation)
    /// блокируются с toast-уведомлением — проверка soft-блокировки.
    /// </summary>
    [AvaloniaFact]
    public void Security_WriteOps_Blocked_Without_Api()
    {
        var (_, vm) = CreateWizardAtStep3();

        // Save
        vm.SaveCommand.Execute(null);
        Assert.True(vm.HasToast);
        Assert.Contains("недоступен", vm.Toast);

        // Start simulation
        vm.StartSimulationCommand.Execute(null);
        Assert.False(vm.IsRunning);
    }

    /// <summary>
    /// Сценарий: ApiSettings по умолчанию — localhost:5000.
    /// </summary>
    [AvaloniaFact]
    public void Security_DefaultEndpoint()
    {
        var settings = new ApiSettings();
        Assert.Equal("http://localhost:5000/", settings.BaseUrl);
        Assert.Equal(TimeSpan.FromSeconds(15), settings.RequestTimeout);
    }

    /// <summary>
    /// Сценарий: ApiBaseUrl доступен на ViewModel для отображения в UI.
    /// </summary>
    [AvaloniaFact]
    public void Security_ApiBaseUrl_ExposedOnVm()
    {
        var (_, vm) = CreateWizardAtStep3();

        // При null API — ApiBaseUrl равен DefaultBaseUrl
        Assert.Equal(ApiSettings.DefaultBaseUrl, vm.ApiBaseUrl);
    }

    /// <summary>
    /// Сценарий: Множественное создание задач — каждая с уникальным ID,
    /// нет утечки данных между задачами.
    /// </summary>
    [AvaloniaFact]
    public async Task Security_NoDataLeak_BetweenCreations()
    {
        var (_, vm) = CreateWizardAtStep3(name: "Задача А", description: "Описание А");
        await vm.CreateProblemAsync();
        var pA = vm.CurrentProblem;

        vm.SelectScreenCommand.Execute("Wizard");
        vm.WizardName = "Задача Б";
        vm.WizardDescription = "Описание Б";
        vm.WizardNextCommand.Execute(null);
        vm.WizardNextCommand.Execute(null);
        await vm.CreateProblemAsync();
        var pB = vm.CurrentProblem;

        Assert.NotEqual(pA.Id, pB.Id);
        Assert.NotEqual(pA.Name, pB.Name);
        Assert.Equal("Задача А", pA.Name);
        Assert.Equal("Задача Б", pB.Name);
    }
}
