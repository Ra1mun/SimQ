using Avalonia.Headless.XUnit;
using SimQ.Client.Models;
using SimQ.Client.ViewModels;
using SimQ.Client.Views;

namespace SimQ.Client.Tests;

/// <summary>
/// Headless UI tests for the Wizard flow.
/// </summary>
public class WizardTests
{
    private static MainViewModel CreateVm()
    {
        var vm = new MainViewModel(null);
        var window = new MainWindow { DataContext = vm };
        window.Show();
        return vm;
    }

    [AvaloniaFact]
    public void Wizard_Defaults_To_Step1()
    {
        var vm = CreateVm();
        vm.SelectScreenCommand.Execute("Wizard");

        Assert.True(vm.IsWizard);
        Assert.Equal(1, vm.WizardStep);
        Assert.True(vm.IsWizardStep1);
        Assert.False(vm.CanWizardBack);
        Assert.True(vm.CanWizardNext);
    }

    [AvaloniaFact]
    public void Wizard_Next_Moves_To_Step2()
    {
        var vm = CreateVm();
        vm.SelectScreenCommand.Execute("Wizard");

        vm.WizardNextCommand.Execute(null);

        Assert.Equal(2, vm.WizardStep);
        Assert.True(vm.IsWizardStep2);
        Assert.True(vm.CanWizardBack);
        Assert.True(vm.CanWizardNext);
    }

    [AvaloniaFact]
    public void Wizard_Next_Next_Reaches_Step3()
    {
        var vm = CreateVm();
        vm.SelectScreenCommand.Execute("Wizard");

        vm.WizardNextCommand.Execute(null);
        vm.WizardNextCommand.Execute(null);

        Assert.Equal(3, vm.WizardStep);
        Assert.True(vm.IsWizardStep3);
        Assert.True(vm.CanWizardBack);
        Assert.False(vm.CanWizardNext);
    }

    [AvaloniaFact]
    public void Wizard_Back_Returns_To_Previous_Step()
    {
        var vm = CreateVm();
        vm.SelectScreenCommand.Execute("Wizard");
        vm.WizardNextCommand.Execute(null); // step 2

        vm.WizardBackCommand.Execute(null);

        Assert.Equal(1, vm.WizardStep);
        Assert.True(vm.IsWizardStep1);
    }

    [AvaloniaFact]
    public void Wizard_Cancel_Returns_To_Editor()
    {
        var vm = CreateVm();
        vm.SelectScreenCommand.Execute("Wizard");
        vm.WizardNextCommand.Execute(null);

        vm.WizardCancelCommand.Execute(null);

        Assert.Equal(AppScreen.Editor, vm.Screen);
        Assert.Equal(1, vm.WizardStep); // reset
    }

    [AvaloniaFact]
    public void Wizard_StepLabel_Updates()
    {
        var vm = CreateVm();
        vm.SelectScreenCommand.Execute("Wizard");

        Assert.Contains("1", vm.WizardStepLabel);

        vm.WizardNextCommand.Execute(null);
        Assert.Contains("2", vm.WizardStepLabel);
    }
}
