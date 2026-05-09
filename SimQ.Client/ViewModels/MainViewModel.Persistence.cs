using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SimQ.Client.Models;
using SimQ.Client.Services;

namespace SimQ.Client.ViewModels;

public partial class MainViewModel
{
    private static readonly JsonSerializerOptions ProblemJsonRead = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly JsonSerializerOptions ProblemJsonWrite = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (_api == null || !IsApiOnline)
            {
                ShowError("API недоступен — задача не отправлена");
                return;
            }

            var request = ProblemMapper.ToRegisterRequest(CurrentProblem.Model);
            var existingId = CurrentProblem.Model.Id;
            var isUpdate = !string.IsNullOrEmpty(existingId)
                && !existingId.StartsWith("p-", StringComparison.Ordinal);

            System.Diagnostics.Debug.WriteLine(
                $"[SAVE] isUpdate={isUpdate}, existingId={existingId}, agents={request.Agents.Count}, links={request.Links.Count}");

            var resp = isUpdate
                ? await _api.UpdateProblemAsync(existingId!, request)
                : await _api.RegisterProblemAsync(request);

            if (resp == null)
            {
                ShowError($"Ошибка сохранения: {_api.LastError ?? "нет ответа"}");
                return;
            }

            if (!string.IsNullOrEmpty(resp.Id))
                CurrentProblem.Model.Id = resp.Id!;

            ShowToast(isUpdate ? $"Обновлено — id {resp.Id}" : $"Сохранено — id {resp.Id}");
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[SAVE] Exception: {ex}");
        }
    }

    [RelayCommand]
    public async Task CreateProblemAsync()
    {
        try
        {
            var name = string.IsNullOrWhiteSpace(WizardName) ? "New problem" : WizardName.Trim();

            var problem = new Problem
            {
                Id          = string.Empty,
                Name        = name,
                Description = WizardDescription,
                CreatedAt   = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                ModifiedAt  = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Status      = ProblemStatus.Draft,
            };

            ProblemTemplates.Apply(problem, (ProblemTemplate)WizardTemplate);

            var vm = new ProblemViewModel(problem);
            
            problem.Id = $"local-{Guid.NewGuid():N}"[..12];
            Problems.Add(vm);
            FilteredProblems.Add(vm);
            CurrentProblem = vm;
            SelectedAgent  = null;
            WizardName        = string.Empty;
            WizardDescription = string.Empty;
            WizardStep        = 1;
            Screen = AppScreen.Editor;
            ShowToast($"Задача создана: {problem.Id}");

            if (_api != null && IsApiOnline)
            {
                try
                {
                    var resp = await _api.RegisterProblemAsync(ProblemMapper.ToRegisterRequest(problem));
                    if (resp != null && !string.IsNullOrEmpty(resp.Id))
                    {
                        problem.Id = resp.Id!;
                        ShowToast($"Сохранено на сервере: {resp.Id}");
                    }
                    else
                    {
                        ShowError($"API: {_api.LastError ?? "нет ответа"}");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"API ошибка: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Не удалось создать задачу: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        var file = await StoragePicker.OpenAsync(
            "Импорт задачи из JSON",
            StoragePicker.Json);
        if (file is null) return;

        try
        {
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();

            var problem = JsonSerializer.Deserialize<Problem>(json, ProblemJsonRead);
            if (problem is null) { ShowError("Не удалось разобрать JSON"); return; }

            if (string.IsNullOrEmpty(problem.Id))
                problem.Id = $"imp-{Guid.NewGuid():N}"[..12];

            problem.RebuildEdgeAnchors();

            var vm = new ProblemViewModel(problem);
            Problems.Add(vm);
            FilteredProblems.Add(vm);
            CurrentProblem = vm;
            SelectedAgent  = vm.Agents.FirstOrDefault();
            Screen = AppScreen.Editor;
            ShowToast($"Импортировано: {problem.Name}");
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка импорта: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportProblemAsync()
    {
        var problem = CurrentProblem.Model;
        var suggestedName = string.IsNullOrWhiteSpace(problem.Name) ? "problem" : problem.Name.Trim();

        var file = await StoragePicker.SaveAsync(
            "Экспорт задачи в JSON",
            "json",
            suggestedName,
            StoragePicker.Json);
        if (file is null) return;

        try
        {
            var json = JsonSerializer.Serialize(problem, ProblemJsonWrite);

            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(json);

            ShowToast("Задача экспортирована");
        }
        catch (Exception ex)
        {
            ShowError($"Ошибка экспорта: {ex.Message}");
        }
    }
}
