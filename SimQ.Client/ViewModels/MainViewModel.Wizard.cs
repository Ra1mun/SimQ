using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimQ.Client.Models;

namespace SimQ.Client.ViewModels;

public partial class MainViewModel
{
    [ObservableProperty] private string _wizardName = "";
    [ObservableProperty] private string _wizardDescription = "";
    [ObservableProperty] private int _wizardTemplate;
    [ObservableProperty] private int _wizardStep = 1;

    [ObservableProperty] private string _wizardTemplateDescription = "";
    [ObservableProperty] private string _wizardTemplateAgents = "—";

    [ObservableProperty] private bool _isWizardStep1 = true;
    [ObservableProperty] private bool _isWizardStep2;
    [ObservableProperty] private bool _isWizardStep3;
    [ObservableProperty] private bool _canWizardBack;
    [ObservableProperty] private bool _canWizardNext = true;

    [ObservableProperty] private string _wizardStepLabel = "ШАГ 1 / 3";
    [ObservableProperty] private string _wizardStepTitle = "Описание задачи";
    [ObservableProperty] private string _wizardStepHint =
        "Имя, описание и шаблон — для последующего поиска в истории.";

    private static readonly IBrush AccentBrushStatic =
        new SolidColorBrush(Color.Parse("#3D5FCC"));
    private static readonly IBrush MutedBrushStatic =
        new SolidColorBrush(Color.Parse("#DFE3EA"));

    [ObservableProperty] private IBrush _wizardStep1Brush = AccentBrushStatic;
    [ObservableProperty] private IBrush _wizardStep2Brush = MutedBrushStatic;
    [ObservableProperty] private IBrush _wizardStep3Brush = MutedBrushStatic;
    [ObservableProperty] private double _wizardStep1Opacity = 1.0;
    [ObservableProperty] private double _wizardStep2Opacity = 0.55;
    [ObservableProperty] private double _wizardStep3Opacity = 0.55;

    private void InitWizardState()
    {
        var (desc, agents) = ProblemTemplates.Describe(ProblemTemplate.Empty);
        WizardTemplateDescription = desc;
        WizardTemplateAgents = agents;
    }

    partial void OnWizardTemplateChanged(int value)
    {
        var template = (ProblemTemplate)value;
        var (description, agents) = ProblemTemplates.Describe(template);
        WizardTemplateDescription = description;
        WizardTemplateAgents = agents;
    }

    partial void OnWizardStepChanged(int value)
    {
        var step = System.Math.Clamp(value, 1, 3);
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
            _ => "",
        };

        WizardStep1Brush   = step == 1 ? AccentBrushStatic : MutedBrushStatic;
        WizardStep2Brush   = step >= 2 ? AccentBrushStatic : MutedBrushStatic;
        WizardStep3Brush   = step >= 3 ? AccentBrushStatic : MutedBrushStatic;
        WizardStep1Opacity = step == 1 ? 1.0 : 0.55;
        WizardStep2Opacity = step == 2 ? 1.0 : 0.55;
        WizardStep3Opacity = step == 3 ? 1.0 : 0.55;
    }

    partial void OnCanWizardBackChanged(bool value) => WizardBackCommand.NotifyCanExecuteChanged();
    partial void OnCanWizardNextChanged(bool value) => WizardNextCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void WizardCancel()
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

    [RelayCommand] private void OpenWizard() => Screen = AppScreen.Wizard;

    public Task WizardCreateAsync() => CreateProblemAsync();
}
