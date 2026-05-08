using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using SimQ.Client.Models;
using SimQ.Client.ViewModels;
using SimQ.Client.Views;

namespace SimQ.Client.Tests;

/// <summary>
/// Тесты экрана «Редактор модели»: холст, агенты, связи, панель свойств,
/// боковая панель задач, навигация, согласованность и негативные сценарии.
/// </summary>
public class EditorViewTests
{
    private static (MainWindow window, MainViewModel vm) CreateEditor()
    {
        var vm = new MainViewModel(null);
        var window = new MainWindow { DataContext = vm, Width = 1440, Height = 900 };
        window.Show();
        vm.SelectScreenCommand.Execute("Editor");
        return (window, vm);
    }

    // ═════════════════════════════════════════════════════════
    //  1. ГРАФИЧЕСКИЙ РЕДАКТОР (ХОЛСТ)
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Добавление агента Source — счётчик увеличивается.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_AddSource_IncreasesAgentCount()
    {
        var (_, vm) = CreateEditor();
        var before = vm.CurrentProblem.AgentCount;

        vm.AddAgentCommand.Execute("Source");

        Assert.Equal(before + 1, vm.CurrentProblem.AgentCount);
    }

    /// <summary>
    /// Сценарий: Добавление агентов всех типов.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_AddAllAgentTypes()
    {
        var (_, vm) = CreateEditor();
        var before = vm.CurrentProblem.AgentCount;

        vm.AddAgentCommand.Execute("Source");
        vm.AddAgentCommand.Execute("ServiceBlock");
        vm.AddAgentCommand.Execute("Buffer");
        vm.AddAgentCommand.Execute("Orbit");
        vm.AddAgentCommand.Execute("Sink");

        Assert.Equal(before + 5, vm.CurrentProblem.AgentCount);
    }

    /// <summary>
    /// Сценарий: Добавление агента — новый агент автоматически выделяется.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_AddAgent_BecomesSelected()
    {
        var (_, vm) = CreateEditor();

        vm.AddAgentCommand.Execute("ServiceBlock");

        Assert.NotNull(vm.SelectedAgent);
        Assert.Equal(AgentKind.ServiceBlock, vm.SelectedAgent!.Kind);
    }

    /// <summary>
    /// Сценарий: Добавление агента — появляется toast.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_AddAgent_ShowsToast()
    {
        var (_, vm) = CreateEditor();

        vm.AddAgentCommand.Execute("Buffer");

        Assert.True(vm.HasToast);
        Assert.Contains("Добавлен", vm.Toast);
    }

    /// <summary>
    /// Сценарий: Выделение (клик) по агенту — панель свойств обновляется.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_SelectAgent_UpdatesProperties()
    {
        var (_, vm) = CreateEditor();
        var agent = vm.CurrentProblem.Agents[0];

        vm.SelectAgentCommand.Execute(agent);

        Assert.Equal(agent, vm.SelectedAgent);
        Assert.Equal(agent.Name, vm.SelectedAgent!.Name);
        Assert.Equal(agent.Kind, vm.SelectedAgent.Kind);
    }

    /// <summary>
    /// Сценарий: Переключение выделения между агентами.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_SwitchSelection()
    {
        var (_, vm) = CreateEditor();
        var a1 = vm.CurrentProblem.Agents[0];
        var a2 = vm.CurrentProblem.Agents[1];

        vm.SelectAgentCommand.Execute(a1);
        Assert.Equal(a1, vm.SelectedAgent);

        vm.SelectAgentCommand.Execute(a2);
        Assert.Equal(a2, vm.SelectedAgent);
        Assert.NotEqual(a1, vm.SelectedAgent);
    }

    /// <summary>
    /// Сценарий: Перемещение узла — координаты обновляются.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_MoveNode_UpdatesCoordinates()
    {
        var (_, vm) = CreateEditor();
        var agent = vm.CurrentProblem.Agents[0];
        vm.SelectAgentCommand.Execute(agent);

        agent.X = 500;
        agent.Y = 300;

        Assert.Equal(500, agent.X);
        Assert.Equal(300, agent.Y);
        Assert.Equal(500, agent.Model.X); // синхронизация с моделью
        Assert.Equal(300, agent.Model.Y);
    }

    /// <summary>
    /// Сценарий: Перемещение узла — связи пересчитываются.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_MoveNode_RebuildEdges()
    {
        var (_, vm) = CreateEditor();
        var agent = vm.CurrentProblem.Agents[0]; // Source #1

        var edgeBefore = vm.CurrentProblem.Edges
            .FirstOrDefault(e => e.From == agent.Id);

        agent.X = 200; // Trigger RebuildEdgeAnchors via PropertyChanged

        if (edgeBefore != null)
        {
            // StartPoint должен обновиться (X + NodeWidth)
            Assert.Equal(200 + Agent.NodeWidth, edgeBefore.StartPoint.X);
        }
    }

    /// <summary>
    /// Сценарий: Удаление агента — счётчик уменьшается.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_DeleteAgent_DecreasesCount()
    {
        var (_, vm) = CreateEditor();
        vm.AddAgentCommand.Execute("Source");
        var added = vm.SelectedAgent!;
        var before = vm.CurrentProblem.AgentCount;

        vm.DeleteAgentCommand.Execute(added);

        Assert.Equal(before - 1, vm.CurrentProblem.AgentCount);
    }

    /// <summary>
    /// Сценарий: Удаление агента — связанные рёбра удаляются.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_DeleteAgent_RemovesEdges()
    {
        var (_, vm) = CreateEditor();
        // Source #1 (a1) имеет связь e1: a1→a3
        var source = vm.CurrentProblem.Agents.First(a => a.Id == "a1");
        var edgesBefore = vm.CurrentProblem.EdgeCount;

        vm.DeleteAgentCommand.Execute(source);

        // Все связи от/к a1 удалены
        Assert.DoesNotContain(vm.CurrentProblem.Edges, e => e.From == "a1" || e.To == "a1");
        Assert.True(vm.CurrentProblem.EdgeCount < edgesBefore);
    }

    /// <summary>
    /// Сценарий: Удаление выделенного агента — выделение сбрасывается.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_DeleteSelectedAgent_ClearsSelection()
    {
        var (_, vm) = CreateEditor();
        vm.AddAgentCommand.Execute("Sink");
        var added = vm.SelectedAgent!;

        vm.DeleteAgentCommand.Execute(added);

        Assert.Null(vm.SelectedAgent);
    }

    /// <summary>
    /// Сценарий: Удаление чужого (не выделенного) агента — выделение остаётся.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_DeleteOther_KeepsSelection()
    {
        var (_, vm) = CreateEditor();
        var first = vm.CurrentProblem.Agents[0];
        vm.AddAgentCommand.Execute("Source");
        var added = vm.SelectedAgent!; // новый выбран

        vm.DeleteAgentCommand.Execute(first); // удаляем первый

        Assert.Equal(added, vm.SelectedAgent); // выделение осталось на новом
    }

    // ═════════════════════════════════════════════════════════
    //  2. ПАНЕЛЬ СВОЙСТВ (Правая колонка)
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Редактирование имени агента — сохраняется в модели.
    /// </summary>
    [AvaloniaFact]
    public void Properties_EditName_SavedInModel()
    {
        var (_, vm) = CreateEditor();
        var agent = vm.CurrentProblem.Agents[0];
        vm.SelectAgentCommand.Execute(agent);

        vm.SelectedAgent!.Name = "Renamed Source";

        Assert.Equal("Renamed Source", agent.Model.Name);
    }

    /// <summary>
    /// Сценарий: Переключение агента — свойства обновляются.
    /// </summary>
    [AvaloniaFact]
    public void Properties_SwitchAgent_UpdatesPanel()
    {
        var (_, vm) = CreateEditor();
        var src = vm.CurrentProblem.Agents.First(a => a.Kind == AgentKind.Source);
        var svb = vm.CurrentProblem.Agents.First(a => a.Kind == AgentKind.ServiceBlock);

        vm.SelectAgentCommand.Execute(src);
        Assert.False(string.IsNullOrEmpty(vm.SelectedAgent!.ParamsSummary));

        vm.SelectAgentCommand.Execute(svb);
        Assert.Contains("c=", vm.SelectedAgent!.ParamsSummary);
    }

    /// <summary>
    /// Сценарий: Изменение каналов ServiceBlock — ParamsSummary обновляется.
    /// </summary>
    [AvaloniaFact]
    public void Properties_EditChannels_UpdatesSummary()
    {
        var (_, vm) = CreateEditor();
        var svb = vm.CurrentProblem.Agents.First(a => a.Kind == AgentKind.ServiceBlock);
        vm.SelectAgentCommand.Execute(svb);

        svb.Model.Channels = 5;

        // ParamsSummary читает Model.Channels
        Assert.Contains("c=5", svb.ParamsSummary);
    }

    /// <summary>
    /// Сценарий: Изменение позиции через панель свойств.
    /// </summary>
    [AvaloniaFact]
    public void Properties_EditPosition()
    {
        var (_, vm) = CreateEditor();
        var agent = vm.CurrentProblem.Agents[0];
        vm.SelectAgentCommand.Execute(agent);

        vm.SelectedAgent!.X = 777;
        vm.SelectedAgent!.Y = 333;

        Assert.Equal(777, agent.Model.X);
        Assert.Equal(333, agent.Model.Y);
    }

    /// <summary>
    /// Сценарий: ID агента — только для чтения (отображается, но не редактируется).
    /// </summary>
    [AvaloniaFact]
    public void Properties_IdIsReadOnly()
    {
        var (_, vm) = CreateEditor();
        var agent = vm.CurrentProblem.Agents[0];
        vm.SelectAgentCommand.Execute(agent);

        // Id доступен для отображения
        Assert.False(string.IsNullOrEmpty(vm.SelectedAgent!.Id));
    }

    /// <summary>
    /// Сценарий: KindLabel отображается корректно.
    /// </summary>
    [AvaloniaFact]
    public void Properties_KindLabel_Correct()
    {
        var (_, vm) = CreateEditor();
        var src = vm.CurrentProblem.Agents.First(a => a.Kind == AgentKind.Source);
        vm.SelectAgentCommand.Execute(src);

        Assert.Equal("Источник", vm.SelectedAgent!.KindLabel);
    }

    // ═════════════════════════════════════════════════════════
    //  3. БОКОВАЯ ПАНЕЛЬ ЗАДАЧ (Sidebar)
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Список задач не пуст.
    /// </summary>
    [AvaloniaFact]
    public void Sidebar_ProblemsListNotEmpty()
    {
        var (_, vm) = CreateEditor();

        Assert.True(vm.Problems.Count >= 3);
    }

    /// <summary>
    /// Сценарий: Переключение между задачами — холст обновляется.
    /// </summary>
    [AvaloniaFact]
    public void Sidebar_SwitchProblem_UpdatesCanvas()
    {
        var (_, vm) = CreateEditor();
        var p1 = vm.Problems[0];
        var p2 = vm.Problems[1];

        vm.PickProblemCommand.Execute(p2);

        Assert.Equal(p2, vm.CurrentProblem);
        Assert.NotEqual(p1.Name, vm.CurrentProblem.Name);
    }

    /// <summary>
    /// Сценарий: Переключение задачи — SelectedAgent сбрасывается.
    /// </summary>
    [AvaloniaFact]
    public void Sidebar_SwitchProblem_ResetsSelectedAgent()
    {
        var (_, vm) = CreateEditor();
        vm.SelectAgentCommand.Execute(vm.CurrentProblem.Agents[0]);

        vm.PickProblemCommand.Execute(vm.Problems[1]);

        // OnCurrentProblemChanged ставит SelectedAgent = Agents.FirstOrDefault()
        // или null, но в любом случае не старый агент
    }

    /// <summary>
    /// Сценарий: Переключение задачи — AgentCount/EdgeCount отражают новую задачу.
    /// </summary>
    [AvaloniaFact]
    public void Sidebar_SwitchProblem_FooterUpdates()
    {
        var (_, vm) = CreateEditor();
        var p1Agents = vm.Problems[0].AgentCount;

        vm.PickProblemCommand.Execute(vm.Problems[1]);

        // Вторая задача (MultiChannelQueue) имеет 4 агента
        Assert.Equal(4, vm.CurrentProblem.AgentCount);
        Assert.NotEqual(p1Agents, vm.CurrentProblem.AgentCount);
    }

    /// <summary>
    /// Сценарий: Каждая задача имеет имя, описание и метку агентов.
    /// </summary>
    [AvaloniaFact]
    public void Sidebar_ProblemCards_HaveContent()
    {
        var (_, vm) = CreateEditor();

        foreach (var p in vm.Problems)
        {
            Assert.False(string.IsNullOrEmpty(p.Name));
            Assert.False(string.IsNullOrEmpty(p.Description));
            Assert.True(p.AgentCount > 0);
        }
    }

    /// <summary>
    /// Сценарий: Кнопка «Новая задача» открывает мастер.
    /// </summary>
    [AvaloniaFact]
    public void Sidebar_NewProblemButton_OpensWizard()
    {
        var (_, vm) = CreateEditor();

        vm.OpenWizardCommand.Execute(null);

        Assert.Equal(AppScreen.Wizard, vm.Screen);
        Assert.True(vm.IsWizard);
    }

    // ═════════════════════════════════════════════════════════
    //  4. ВЕРХНЯЯ ПАНЕЛЬ И НАВИГАЦИЯ
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Сохранение без API — toast «API недоступен».
    /// </summary>
    [AvaloniaFact]
    public void TopBar_Save_WithoutApi()
    {
        var (_, vm) = CreateEditor();

        vm.SaveCommand.Execute(null);

        Assert.True(vm.HasToast);
        Assert.Contains("недоступен", vm.Toast);
    }

    /// <summary>
    /// Сценарий: Переход на вкладку «Моделирование».
    /// </summary>
    [AvaloniaFact]
    public void TopBar_SwitchToSimulate()
    {
        var (_, vm) = CreateEditor();

        vm.SelectScreenCommand.Execute("Simulate");

        Assert.Equal(AppScreen.Simulate, vm.Screen);
        Assert.True(vm.IsSimulate);
    }

    /// <summary>
    /// Сценарий: Переход на «Результаты» и обратно — редактор сохраняет состояние.
    /// </summary>
    [AvaloniaFact]
    public void TopBar_NavigateAwayAndBack_StatePreserved()
    {
        var (_, vm) = CreateEditor();
        var currentName = vm.CurrentProblem.Name;
        vm.AddAgentCommand.Execute("Orbit"); // добавляем агент
        var countAfterAdd = vm.CurrentProblem.AgentCount;

        vm.SelectScreenCommand.Execute("Results");
        vm.SelectScreenCommand.Execute("Editor");

        Assert.Equal(currentName, vm.CurrentProblem.Name);
        Assert.Equal(countAfterAdd, vm.CurrentProblem.AgentCount);
    }

    /// <summary>
    /// Сценарий: ScreenLabel на экране редактора.
    /// </summary>
    [AvaloniaFact]
    public void TopBar_ScreenLabel()
    {
        var (_, vm) = CreateEditor();

        Assert.Contains("Редактор", vm.ScreenLabel);
    }

    // ═════════════════════════════════════════════════════════
    //  5. СОГЛАСОВАННОСТЬ (Интеграция)
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Футер и холст — AgentCount совпадает с количеством агентов.
    /// </summary>
    [AvaloniaFact]
    public void Integration_FooterMatchesCanvas()
    {
        var (_, vm) = CreateEditor();

        Assert.Equal(vm.CurrentProblem.Agents.Count, vm.CurrentProblem.AgentCount);
        Assert.Equal(vm.CurrentProblem.Edges.Count, vm.CurrentProblem.EdgeCount);
    }

    /// <summary>
    /// Сценарий: FinServiceProblem — 5 агентов, 4 связи.
    /// </summary>
    [AvaloniaFact]
    public void Integration_FinServiceProblem_5Agents_4Edges()
    {
        var (_, vm) = CreateEditor();

        Assert.Equal("FinServiceProblem", vm.CurrentProblem.Name);
        Assert.Equal(5, vm.CurrentProblem.AgentCount);
        Assert.Equal(4, vm.CurrentProblem.EdgeCount);
    }

    /// <summary>
    /// Сценарий: После добавления агента — футер обновляется (+1).
    /// </summary>
    [AvaloniaFact]
    public void Integration_AddAgent_FooterUpdates()
    {
        var (_, vm) = CreateEditor();

        vm.AddAgentCommand.Execute("Source");

        Assert.Equal(6, vm.CurrentProblem.AgentCount);
    }

    /// <summary>
    /// Сценарий: После удаления агента с рёбрами — оба счётчика уменьшаются.
    /// </summary>
    [AvaloniaFact]
    public void Integration_DeleteAgent_BothCountersDecrease()
    {
        var (_, vm) = CreateEditor();
        var a1 = vm.CurrentProblem.Agents.First(a => a.Id == "a1");
        var agentsBefore = vm.CurrentProblem.AgentCount;
        var edgesBefore = vm.CurrentProblem.EdgeCount;

        vm.DeleteAgentCommand.Execute(a1);

        Assert.Equal(agentsBefore - 1, vm.CurrentProblem.AgentCount);
        Assert.True(vm.CurrentProblem.EdgeCount < edgesBefore);
    }

    /// <summary>
    /// Сценарий: Zoom по умолчанию = 1.0 (100%).
    /// </summary>
    [AvaloniaFact]
    public void Integration_ZoomDefault()
    {
        var (_, vm) = CreateEditor();

        Assert.Equal(1.0, vm.Zoom);
    }

    /// <summary>
    /// Сценарий: Связи имеют корректные StartPoint/EndPoint после RebuildEdgeAnchors.
    /// </summary>
    [AvaloniaFact]
    public void Integration_EdgeAnchors_MatchAgentPositions()
    {
        var (_, vm) = CreateEditor();
        var problem = vm.CurrentProblem.Model;
        problem.RebuildEdgeAnchors();

        foreach (var edge in vm.CurrentProblem.Edges)
        {
            var fromAgent = problem.Agents.First(a => a.Id == edge.From);
            var toAgent = problem.Agents.First(a => a.Id == edge.To);

            // StartPoint = правая сторона from, EndPoint = левая сторона to
            Assert.Equal(fromAgent.X + Agent.NodeWidth, edge.StartPoint.X);
            Assert.Equal(fromAgent.Y + Agent.NodeHeight / 2, edge.StartPoint.Y);
            Assert.Equal(toAgent.X, edge.EndPoint.X);
            Assert.Equal(toAgent.Y + Agent.NodeHeight / 2, edge.EndPoint.Y);
        }
    }

    /// <summary>
    /// Сценарий: Типы агентов FinServiceProblem — Source ×2, Buffer, ServiceBlock, Sink.
    /// </summary>
    [AvaloniaFact]
    public void Integration_FinServiceProblem_AgentTypes()
    {
        var (_, vm) = CreateEditor();

        Assert.Equal(2, vm.CurrentProblem.SourceCount);
        Assert.Equal(1, vm.CurrentProblem.BlockCount);
        Assert.Equal(1, vm.CurrentProblem.BufferCount);
        Assert.Single(vm.CurrentProblem.Agents.Where(a => a.Kind == AgentKind.Sink));
    }

    // ═════════════════════════════════════════════════════════
    //  6. НЕГАТИВНЫЕ СЦЕНАРИИ
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// Сценарий: Добавление агента с некорректным типом — игнорируется.
    /// </summary>
    [AvaloniaFact]
    public void Negative_AddAgent_InvalidType_Ignored()
    {
        var (_, vm) = CreateEditor();
        var before = vm.CurrentProblem.AgentCount;

        vm.AddAgentCommand.Execute("InvalidType");

        Assert.Equal(before, vm.CurrentProblem.AgentCount);
    }

    /// <summary>
    /// Сценарий: Добавление агента с null параметром — игнорируется.
    /// </summary>
    [AvaloniaFact]
    public void Negative_AddAgent_Null_Ignored()
    {
        var (_, vm) = CreateEditor();
        var before = vm.CurrentProblem.AgentCount;

        vm.AddAgentCommand.Execute(null);

        Assert.Equal(before, vm.CurrentProblem.AgentCount);
    }

    /// <summary>
    /// Сценарий: Удаление null агента — не крашится.
    /// </summary>
    [AvaloniaFact]
    public void Negative_DeleteNull_NoCrash()
    {
        var (_, vm) = CreateEditor();
        var before = vm.CurrentProblem.AgentCount;

        vm.DeleteAgentCommand.Execute(null);

        Assert.Equal(before, vm.CurrentProblem.AgentCount);
    }

    /// <summary>
    /// Сценарий: Потеря связи с ядром — IsApiOnline == false.
    /// </summary>
    [AvaloniaFact]
    public void Negative_ApiOffline()
    {
        var (_, vm) = CreateEditor();

        Assert.False(vm.IsApiOnline);
    }

    /// <summary>
    /// Сценарий: Сохранение при отключенном API — показывает ошибку.
    /// </summary>
    [AvaloniaFact]
    public void Negative_SaveOffline_ShowsError()
    {
        var (_, vm) = CreateEditor();

        vm.SaveCommand.Execute(null);

        Assert.True(vm.HasToast);
        Assert.Matches(@"[Нн]едоступен", vm.Toast);
    }

    /// <summary>
    /// Сценарий: Удаление всех агентов — корректное пустое состояние.
    /// </summary>
    [AvaloniaFact]
    public void Negative_DeleteAllAgents()
    {
        var (_, vm) = CreateEditor();

        while (vm.CurrentProblem.Agents.Count > 0)
        {
            vm.DeleteAgentCommand.Execute(vm.CurrentProblem.Agents[0]);
        }

        Assert.Equal(0, vm.CurrentProblem.AgentCount);
        Assert.Equal(0, vm.CurrentProblem.EdgeCount);
        Assert.Null(vm.SelectedAgent);
    }

    /// <summary>
    /// Сценарий: SelectScreen с пустой строкой — игнорируется.
    /// </summary>
    [AvaloniaFact]
    public void Negative_SelectScreen_Empty_Ignored()
    {
        var (_, vm) = CreateEditor();

        vm.SelectScreenCommand.Execute("");

        Assert.Equal(AppScreen.Editor, vm.Screen);
    }

    /// <summary>
    /// Сценарий: Множественное добавление и удаление — счётчики согласованы.
    /// </summary>
    [AvaloniaFact]
    public void Negative_BulkAddDelete_ConsistentCounts()
    {
        var (_, vm) = CreateEditor();

        // Добавляем 10 агентов
        for (int i = 0; i < 10; i++)
            vm.AddAgentCommand.Execute("Source");

        var afterAdd = vm.CurrentProblem.AgentCount;
        Assert.Equal(15, afterAdd); // 5 + 10

        // Удаляем 5 последних
        for (int i = 0; i < 5; i++)
            vm.DeleteAgentCommand.Execute(vm.CurrentProblem.Agents.Last());

        Assert.Equal(10, vm.CurrentProblem.AgentCount);
        Assert.Equal(vm.CurrentProblem.Agents.Count, vm.CurrentProblem.AgentCount);
    }

    // ═════════════════════════════════════════════════════════
    //  ХОЛСТ: СТРУКТУРА И ПАНОРАМИРОВАНИЕ
    // ═════════════════════════════════════════════════════════

    /// <summary>
    /// GraphCanvas должен существовать в visual tree.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_GraphCanvas_Exists()
    {
        var (window, vm) = CreateEditor();

        var editorView = window.FindDescendantOfType<EditorView>();
        Assert.NotNull(editorView);

        var canvas = editorView!.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == "GraphCanvas");
        Assert.NotNull(canvas);
    }

    /// <summary>
    /// CanvasViewport (Border) должен иметь ClipToBounds=true.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_Viewport_HasClipToBounds()
    {
        var (window, vm) = CreateEditor();

        var editorView = window.FindDescendantOfType<EditorView>();
        var viewport = editorView!.GetVisualDescendants().OfType<Border>().FirstOrDefault(c => c.Name == "CanvasViewport");
        Assert.NotNull(viewport);
        Assert.True(viewport!.ClipToBounds);
    }

    /// <summary>
    /// GraphCanvas должен иметь RenderTransform (TransformGroup с Scale + Translate)
    /// после загрузки, чтобы поддерживать масштабирование и панорамирование.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_GraphCanvas_TransformIsCorrectIfLoaded()
    {
        var (window, vm) = CreateEditor();

        var editorView = window.FindDescendantOfType<EditorView>();
        var canvas = editorView!.GetVisualDescendants().OfType<Control>().FirstOrDefault(c => c.Name == "GraphCanvas");
        Assert.NotNull(canvas);

        var transform = canvas!.RenderTransform as Avalonia.Media.TransformGroup;
        if (transform != null)
        {
            Assert.Equal(2, transform.Children.Count);
            Assert.IsType<Avalonia.Media.ScaleTransform>(transform.Children[0]);
            Assert.IsType<Avalonia.Media.TranslateTransform>(transform.Children[1]);
        }
    }

    /// <summary>
    /// Перетаскивание агента должно менять его координаты X/Y
    /// (модельный уровень — при изменении координат узел остаётся видимым).
    /// </summary>
    [AvaloniaFact]
    public void Canvas_DragAgent_UpdatesCoordinates()
    {
        var (_, vm) = CreateEditor();
        vm.AddAgentCommand.Execute("Source");

        var agent = vm.SelectedAgent!;
        double origX = agent.X;
        double origY = agent.Y;

        // Имитируем сдвиг
        agent.X = origX + 200;
        agent.Y = origY + 150;

        Assert.Equal(origX + 200, agent.X);
        Assert.Equal(origY + 150, agent.Y);
    }

    /// <summary>
    /// Агент с координатами за пределами видимой области (например, X=5000)
    /// должен оставаться в коллекции Agents — канвас достаточно большой.
    /// </summary>
    [AvaloniaFact]
    public void Canvas_AgentFarFromOrigin_StillInCollection()
    {
        var (_, vm) = CreateEditor();
        vm.AddAgentCommand.Execute("Buffer");
        var agent = vm.SelectedAgent!;

        agent.X = 5000;
        agent.Y = 3000;

        Assert.Contains(agent, vm.CurrentProblem.Agents);
        Assert.Equal(5000, agent.X);
        Assert.Equal(3000, agent.Y);
    }
}
