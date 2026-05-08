using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using SimQ.Client.Models;
using SimQ.Client.ViewModels;
using SimQ.Client.Views;

namespace SimQ.Client.Tests;

/// <summary>
/// Комплексный набор UI-тестов мастера создания задачи (Wizard).
/// Покрывает: функциональное тестирование выбора шаблона, навигацию,
/// UI/доступность, негативные сценарии и локализацию.
/// </summary>
public class WizardScenarioTests
{
    // ─────────────────────────────────────────────────────────
    //  Утилиты
    // ─────────────────────────────────────────────────────────

    private static (MainWindow window, MainViewModel vm) CreateWizard()
    {
        var vm = new MainViewModel(null); // API offline
        var window = new MainWindow { DataContext = vm, Width = 1440, Height = 900 };
        window.Show();
        vm.SelectScreenCommand.Execute("Wizard");
        return (window, vm);
    }

    // ═════════════════════════════════════════════════════════
    //  1. ФУНКЦИОНАЛЬНОЕ ТЕСТИРОВАНИЕ (Выбор шаблона)
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Выбор шаблона по умолчанию.
    /// Ожидаемый результат: При открытии мастера первый шаблон
    /// ("Пусто") выбран по умолчанию (SelectedIndex == 0).
    /// </summary>
    [AvaloniaFact]
    public void Template_Default_Selection_Is_First_Item()
    {
        var (_, vm) = CreateWizard();

        Assert.Equal(0, vm.WizardTemplate);
    }

    /// <summary>
    /// Сценарий: Смена выбранного шаблона.
    /// Действие: Программно переключить шаблон на другой.
    /// Ожидаемый результат: WizardTemplate обновляется корректно.
    /// </summary>
    [AvaloniaFact]
    public void Template_Change_Selection()
    {
        var (_, vm) = CreateWizard();

        vm.WizardTemplate = 1; // "M/M/1 базовый"
        Assert.Equal(1, vm.WizardTemplate);

        vm.WizardTemplate = 2; // "M/M/c с очередью"
        Assert.Equal(2, vm.WizardTemplate);

        vm.WizardTemplate = 3; // "С орбитой повторов"
        Assert.Equal(3, vm.WizardTemplate);
    }

    /// <summary>
    /// Сценарий: Информативность шаблонов.
    /// Ожидаемый результат: Доступны 4 варианта шаблонов (индексы 0..3).
    /// Шаблон можно выбрать программно — все значения валидны.
    /// </summary>
    [AvaloniaFact]
    public void Template_All_Four_Options_Selectable()
    {
        var (_, vm) = CreateWizard();

        // Все 4 шаблона доступны для выбора
        for (int i = 0; i < 4; i++)
        {
            vm.WizardTemplate = i;
            Assert.Equal(i, vm.WizardTemplate);
        }
    }

    /// <summary>
    /// Сценарий: Клик по пустому пространству (программный аналог).
    /// Действие: Выбрать шаблон, затем не менять его.
    /// Ожидаемый результат: Выбор шаблона не сбрасывается.
    /// </summary>
    [AvaloniaFact]
    public void Template_Selection_Persists_After_No_Action()
    {
        var (_, vm) = CreateWizard();

        vm.WizardTemplate = 2;
        // Имитация «ничего не происходит» — выбор сохраняется
        Assert.Equal(2, vm.WizardTemplate);
    }

    /// <summary>
    /// Сценарий: Выбор шаблона сохраняется при навигации Назад/Далее.
    /// </summary>
    [AvaloniaFact]
    public void Template_Selection_Preserved_Across_Steps()
    {
        var (_, vm) = CreateWizard();

        vm.WizardTemplate = 2;
        vm.WizardNextCommand.Execute(null); // → шаг 2
        vm.WizardBackCommand.Execute(null); // ← шаг 1

        Assert.Equal(2, vm.WizardTemplate);
    }

    // ═════════════════════════════════════════════════════════
    //  2. НАВИГАЦИЯ И ПЕРЕХОДЫ
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Переход к следующему шагу.
    /// Действие: Выбрать шаблон → нажать «Далее».
    /// Ожидаемый результат: Шаг == 2, прогресс обновляется.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_Next_Goes_To_Step2()
    {
        var (_, vm) = CreateWizard();

        vm.WizardTemplate = 1;
        vm.WizardNextCommand.Execute(null);

        Assert.Equal(2, vm.WizardStep);
        Assert.True(vm.IsWizardStep2);
        Assert.Contains("2", vm.WizardStepLabel);
        Assert.Equal("Структура агентов", vm.WizardStepTitle);
    }

    /// <summary>
    /// Сценарий: Полный проход через все шаги (1 → 2 → 3).
    /// </summary>
    [AvaloniaFact]
    public void Navigation_Full_Forward_To_Step3()
    {
        var (_, vm) = CreateWizard();

        vm.WizardNextCommand.Execute(null); // → 2
        vm.WizardNextCommand.Execute(null); // → 3

        Assert.Equal(3, vm.WizardStep);
        Assert.True(vm.IsWizardStep3);
        Assert.Contains("3", vm.WizardStepLabel);
        Assert.Equal("Параметры моделирования", vm.WizardStepTitle);
        Assert.False(vm.CanWizardNext); // на последнем шаге «Далее» недоступно
    }

    /// <summary>
    /// Сценарий: Отмена создания задачи.
    /// Действие: Нажать «Отмена».
    /// Ожидаемый результат: Возврат на экран редактора, шаг сбрасывается.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_Cancel_Returns_To_Editor()
    {
        var (_, vm) = CreateWizard();
        vm.WizardNextCommand.Execute(null); // шаг 2

        vm.WizardCancelCommand.Execute(null);

        Assert.Equal(AppScreen.Editor, vm.Screen);
        Assert.True(vm.IsEditor);
        Assert.Equal(1, vm.WizardStep); // шаг сбросился
    }

    /// <summary>
    /// Сценарий: Использование вкладок верхнего меню.
    /// Действие: Нажать «Результаты» во время выбора шаблона.
    /// Ожидаемый результат: Переход в раздел результатов.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_Tab_To_Results_During_Wizard()
    {
        var (_, vm) = CreateWizard();
        vm.WizardName = "Тестовая задача";
        vm.WizardTemplate = 2;

        vm.SelectScreenCommand.Execute("Results");

        Assert.Equal(AppScreen.Results, vm.Screen);
        Assert.True(vm.IsResults);
        Assert.False(vm.IsWizard);
    }

    /// <summary>
    /// Сценарий: Возврат в мастер после переключения вкладки —
    /// состояние мастера сохраняется (имя, шаблон).
    /// </summary>
    [AvaloniaFact]
    public void Navigation_Wizard_State_Preserved_After_Tab_Switch()
    {
        var (_, vm) = CreateWizard();
        vm.WizardName = "Моя задача";
        vm.WizardTemplate = 3;

        vm.SelectScreenCommand.Execute("Editor");
        vm.SelectScreenCommand.Execute("Wizard");

        Assert.Equal("Моя задача", vm.WizardName);
        Assert.Equal(3, vm.WizardTemplate);
    }

    /// <summary>
    /// Сценарий: Переход через вкладку «История запусков» во время мастера.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_Tab_To_Tasks_During_Wizard()
    {
        var (_, vm) = CreateWizard();

        vm.SelectScreenCommand.Execute("Tasks");

        Assert.Equal(AppScreen.Tasks, vm.Screen);
        Assert.True(vm.IsTasks);
    }

    /// <summary>
    /// Сценарий: Переход через вкладку «Моделирование» во время мастера.
    /// </summary>
    [AvaloniaFact]
    public void Navigation_Tab_To_Simulate_During_Wizard()
    {
        var (_, vm) = CreateWizard();

        vm.SelectScreenCommand.Execute("Simulate");

        Assert.Equal(AppScreen.Simulate, vm.Screen);
        Assert.True(vm.IsSimulate);
    }

    // ═════════════════════════════════════════════════════════
    //  3. ТЕСТИРОВАНИЕ UI И ДОСТУПНОСТИ
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Визуальное выделение активного шага.
    /// Ожидаемый результат: Шаг 1 подсвечен фиолетовым (акцентным),
    /// шаги 2 и 3 — серым.
    /// </summary>
    [AvaloniaFact]
    public void UI_Step1_Highlighted_Others_Muted()
    {
        var (_, vm) = CreateWizard();

        // Шаг 1 — акцентный цвет (#3D5FCC)
        Assert.IsType<SolidColorBrush>(vm.WizardStep1Brush);
        var step1Color = ((SolidColorBrush)vm.WizardStep1Brush).Color;
        Assert.Equal(Color.Parse("#3D5FCC"), step1Color);

        // Шаг 2, 3 — приглушённый цвет (#DFE3EA)
        var step2Color = ((SolidColorBrush)vm.WizardStep2Brush).Color;
        var step3Color = ((SolidColorBrush)vm.WizardStep3Brush).Color;
        Assert.Equal(Color.Parse("#DFE3EA"), step2Color);
        Assert.Equal(Color.Parse("#DFE3EA"), step3Color);
    }

    /// <summary>
    /// Сценарий: После перехода на шаг 2, индикаторы шагов 1 и 2
    /// становятся акцентными, шаг 3 остаётся серым.
    /// </summary>
    [AvaloniaFact]
    public void UI_Step2_Updates_Progress_Brushes()
    {
        var (_, vm) = CreateWizard();
        vm.WizardNextCommand.Execute(null);

        var step1Color = ((SolidColorBrush)vm.WizardStep1Brush).Color;
        var step2Color = ((SolidColorBrush)vm.WizardStep2Brush).Color;
        var step3Color = ((SolidColorBrush)vm.WizardStep3Brush).Color;

        Assert.Equal(Color.Parse("#3D5FCC"), step1Color);
        Assert.Equal(Color.Parse("#3D5FCC"), step2Color);
        Assert.Equal(Color.Parse("#DFE3EA"), step3Color);
    }

    /// <summary>
    /// Сценарий: Прозрачность (Opacity) текущего шага == 1.0,
    /// остальных == 0.55.
    /// </summary>
    [AvaloniaFact]
    public void UI_Current_Step_Has_Full_Opacity()
    {
        var (_, vm) = CreateWizard();

        Assert.Equal(1.0, vm.WizardStep1Opacity);
        Assert.Equal(0.55, vm.WizardStep2Opacity);
        Assert.Equal(0.55, vm.WizardStep3Opacity);

        vm.WizardNextCommand.Execute(null); // → шаг 2
        Assert.Equal(0.55, vm.WizardStep1Opacity);
        Assert.Equal(1.0, vm.WizardStep2Opacity);
        Assert.Equal(0.55, vm.WizardStep3Opacity);

        vm.WizardNextCommand.Execute(null); // → шаг 3
        Assert.Equal(0.55, vm.WizardStep1Opacity);
        Assert.Equal(0.55, vm.WizardStep2Opacity);
        Assert.Equal(1.0, vm.WizardStep3Opacity);
    }

    /// <summary>
    /// Сценарий: Кнопка «Назад» заблокирована на шаге 1.
    /// </summary>
    [AvaloniaFact]
    public void UI_Back_Button_Disabled_On_Step1()
    {
        var (_, vm) = CreateWizard();

        Assert.False(vm.CanWizardBack);
        Assert.False(vm.WizardBackCommand.CanExecute(null));
    }

    /// <summary>
    /// Сценарий: Кнопка «Далее» доступна на шагах 1 и 2,
    /// недоступна на шаге 3.
    /// </summary>
    [AvaloniaFact]
    public void UI_Next_Button_Availability()
    {
        var (_, vm) = CreateWizard();

        Assert.True(vm.CanWizardNext);   // шаг 1

        vm.WizardNextCommand.Execute(null);
        Assert.True(vm.CanWizardNext);   // шаг 2

        vm.WizardNextCommand.Execute(null);
        Assert.False(vm.CanWizardNext);  // шаг 3
    }

    /// <summary>
    /// Сценарий: Окно рендерится с корректными размерами.
    /// </summary>
    [AvaloniaFact]
    public void UI_Window_Renders_With_Correct_Size()
    {
        var (window, _) = CreateWizard();

        Assert.True(window.Width >= 1100);
        Assert.True(window.Height >= 700);
    }

    /// <summary>
    /// Сценарий: Состояние кнопки «Сохранить» без API.
    /// Ожидаемый результат: Команда не крашится, показывается toast.
    /// </summary>
    [AvaloniaFact]
    public void UI_Save_Command_Without_Api_Shows_Toast()
    {
        var (_, vm) = CreateWizard();

        vm.SaveCommand.Execute(null);

        Assert.True(vm.HasToast);
        Assert.Contains("недоступен", vm.Toast);
    }

    /// <summary>
    /// Сценарий: Подсказка (Hint) обновляется при смене шагов.
    /// </summary>
    [AvaloniaFact]
    public void UI_Hint_Text_Changes_Per_Step()
    {
        var (_, vm) = CreateWizard();

        var hint1 = vm.WizardStepHint;
        Assert.False(string.IsNullOrEmpty(hint1));

        vm.WizardNextCommand.Execute(null);
        var hint2 = vm.WizardStepHint;
        Assert.False(string.IsNullOrEmpty(hint2));
        Assert.NotEqual(hint1, hint2);

        vm.WizardNextCommand.Execute(null);
        var hint3 = vm.WizardStepHint;
        Assert.NotEqual(hint2, hint3);
    }

    // ═════════════════════════════════════════════════════════
    //  4. НЕГАТИВНЫЕ СЦЕНАРИИ И ГРАНИЧНЫЕ УСЛОВИЯ
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Создание задачи с пустым именем.
    /// Ожидаемый результат: Задача создаётся с именем по умолчанию "New problem".
    /// </summary>
    [AvaloniaFact]
    public async Task Negative_Create_With_Empty_Name_Uses_Default()
    {
        var (_, vm) = CreateWizard();

        vm.WizardName = "";
        vm.WizardDescription = "";
        vm.WizardNextCommand.Execute(null);
        vm.WizardNextCommand.Execute(null);

        var countBefore = vm.Problems.Count;
        await vm.CreateProblemAsync();

        Assert.Equal(countBefore + 1, vm.Problems.Count);
        var created = vm.Problems.Last();
        Assert.Equal("New problem", created.Name);
    }

    /// <summary>
    /// Сценарий: Создание задачи с заполненным именем.
    /// </summary>
    [AvaloniaFact]
    public async Task Negative_Create_With_Custom_Name()
    {
        var (_, vm) = CreateWizard();

        vm.WizardName = "  Тестовая M/M/1  ";
        vm.WizardNextCommand.Execute(null);
        vm.WizardNextCommand.Execute(null);

        await vm.CreateProblemAsync();

        var created = vm.CurrentProblem;
        Assert.Equal("Тестовая M/M/1", created.Name); // пробелы обрезаны
    }

    /// <summary>
    /// Сценарий: Проверка статуса сервера — API недоступен.
    /// Ожидаемый результат: IsApiOnline == false, StatusText содержит «недоступен» или информацию.
    /// </summary>
    [AvaloniaFact]
    public void Negative_Api_Offline_Status()
    {
        var (_, vm) = CreateWizard();

        Assert.False(vm.IsApiOnline);
    }

    /// <summary>
    /// Сценарий: Запуск симуляции без API — блокируется.
    /// </summary>
    [AvaloniaFact]
    public void Negative_StartSimulation_Without_Api_Blocked()
    {
        var (_, vm) = CreateWizard();
        vm.SelectScreenCommand.Execute("Simulate");

        vm.StartSimulationCommand.Execute(null);

        Assert.False(vm.IsRunning);
        Assert.True(vm.HasToast);
    }

    /// <summary>
    /// Сценарий: Нажатие «Назад» на шаге 1 не меняет состояние
    /// (команда заблокирована).
    /// </summary>
    [AvaloniaFact]
    public void Negative_Back_On_Step1_No_Effect()
    {
        var (_, vm) = CreateWizard();

        Assert.False(vm.WizardBackCommand.CanExecute(null));
        Assert.Equal(1, vm.WizardStep);
    }

    /// <summary>
    /// Сценарий: Нажатие «Далее» на шаге 3 не меняет состояние.
    /// </summary>
    [AvaloniaFact]
    public void Negative_Next_On_Step3_No_Effect()
    {
        var (_, vm) = CreateWizard();
        vm.WizardNextCommand.Execute(null); // → 2
        vm.WizardNextCommand.Execute(null); // → 3

        Assert.False(vm.WizardNextCommand.CanExecute(null));
        Assert.Equal(3, vm.WizardStep);
    }

    /// <summary>
    /// Сценарий: После создания задачи мастер сбрасывается.
    /// </summary>
    [AvaloniaFact]
    public async Task Negative_After_Create_Wizard_Resets()
    {
        var (_, vm) = CreateWizard();
        vm.WizardName = "Тест";
        vm.WizardNextCommand.Execute(null);
        vm.WizardNextCommand.Execute(null);

        await vm.CreateProblemAsync();

        Assert.Equal(AppScreen.Editor, vm.Screen); // вернулся в редактор
        Assert.Equal(1, vm.WizardStep);            // шаг сброшен
        Assert.Equal("", vm.WizardName);            // имя очищено
    }

    /// <summary>
    /// Сценарий: Создание задачи автоматически делает её текущей.
    /// </summary>
    [AvaloniaFact]
    public async Task Negative_Created_Problem_Becomes_Current()
    {
        var (_, vm) = CreateWizard();
        vm.WizardName = "Новая задача";

        await vm.CreateProblemAsync();

        Assert.Equal("Новая задача", vm.CurrentProblem.Name);
    }

    /// <summary>
    /// Сценарий: Созданная задача имеет статус Draft.
    /// </summary>
    [AvaloniaFact]
    public async Task Negative_Created_Problem_Has_Draft_Status()
    {
        var (_, vm) = CreateWizard();
        vm.WizardName = "Черновик";

        await vm.CreateProblemAsync();

        Assert.Equal(ProblemStatus.Draft, vm.CurrentProblem.Status);
    }

    // ═════════════════════════════════════════════════════════
    //  5. ЛОКАЛИЗАЦИЯ
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Заголовок мастера на русском языке.
    /// </summary>
    [AvaloniaFact]
    public void Localization_WizardStepTitle_Is_Russian()
    {
        var (_, vm) = CreateWizard();

        Assert.Equal("Описание задачи", vm.WizardStepTitle);

        vm.WizardNextCommand.Execute(null);
        Assert.Equal("Структура агентов", vm.WizardStepTitle);

        vm.WizardNextCommand.Execute(null);
        Assert.Equal("Параметры моделирования", vm.WizardStepTitle);
    }

    /// <summary>
    /// Сценарий: Метка шага на русском языке в формате «ШАГ N / 3».
    /// </summary>
    [AvaloniaFact]
    public void Localization_StepLabel_Format()
    {
        var (_, vm) = CreateWizard();

        Assert.Equal("ШАГ 1 / 3", vm.WizardStepLabel);

        vm.WizardNextCommand.Execute(null);
        Assert.Equal("ШАГ 2 / 3", vm.WizardStepLabel);

        vm.WizardNextCommand.Execute(null);
        Assert.Equal("ШАГ 3 / 3", vm.WizardStepLabel);
    }

    /// <summary>
    /// Сценарий: Подсказки (Hint) содержат русский текст, без «битых» символов.
    /// </summary>
    [AvaloniaFact]
    public void Localization_Hints_Are_Russian_Text()
    {
        var (_, vm) = CreateWizard();

        // Проверяем что подсказки содержат кириллические символы
        Assert.Matches(@"[а-яА-ЯёЁ]", vm.WizardStepHint);

        vm.WizardNextCommand.Execute(null);
        Assert.Matches(@"[а-яА-ЯёЁ]", vm.WizardStepHint);

        vm.WizardNextCommand.Execute(null);
        Assert.Matches(@"[а-яА-ЯёЁ]", vm.WizardStepHint);
    }

    /// <summary>
    /// Сценарий: Заголовок окна на русском языке.
    /// </summary>
    [AvaloniaFact]
    public void Localization_Window_Title_Is_Russian()
    {
        var (window, _) = CreateWizard();

        Assert.Contains("Клиент моделирования", window.Title);
    }

    /// <summary>
    /// Сценарий: ScreenLabel для мастера на русском языке.
    /// </summary>
    [AvaloniaFact]
    public void Localization_ScreenLabel_For_Wizard()
    {
        var (_, vm) = CreateWizard();

        Assert.Contains("Новая задача", vm.ScreenLabel);
    }

    /// <summary>
    /// Сценарий: Метки экранов на русском языке.
    /// </summary>
    [AvaloniaFact]
    public void Localization_All_ScreenLabels_Are_Russian()
    {
        var (_, vm) = CreateWizard();

        vm.SelectScreenCommand.Execute("Editor");
        Assert.Matches(@"[а-яА-ЯёЁ]", vm.ScreenLabel);

        vm.SelectScreenCommand.Execute("Simulate");
        Assert.Matches(@"[а-яА-ЯёЁ]", vm.ScreenLabel);

        vm.SelectScreenCommand.Execute("Results");
        Assert.Matches(@"[а-яА-ЯёЁ]", vm.ScreenLabel);

        vm.SelectScreenCommand.Execute("Tasks");
        Assert.Matches(@"[а-яА-ЯёЁ]", vm.ScreenLabel);
    }

    /// <summary>
    /// Сценарий: Toast-уведомление при сохранении без API на русском.
    /// </summary>
    [AvaloniaFact]
    public void Localization_Toast_Messages_Are_Russian()
    {
        var (_, vm) = CreateWizard();

        vm.SaveCommand.Execute(null);

        Assert.Matches(@"[а-яА-ЯёЁ]", vm.Toast);
    }
}
