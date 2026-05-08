using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using SimQ.Client.Models;
using SimQ.Client.ViewModels;

namespace SimQ.Client.Views;

public partial class EditorView : UserControl
{
    private AgentViewModel? _dragging;
    private Point _dragStart;
    private double _startX, _startY;

    // Pan state (middle mouse button)
    private bool _isPanning;
    private Point _panStart;
    private double _panOffsetX, _panOffsetY;
    private double _currentPanX, _currentPanY;

    // Link drawing state (right-click on output connector)
    private bool _isLinking;
    private AgentViewModel? _linkSource;
    private Line? _linkPreviewLine;

    private ScaleTransform _scaleTransform = new(1, 1);
    private TranslateTransform _translateTransform = new();
    private Control? _canvasPanel;
    private Control? _viewport; // Border that clips the canvas

    public EditorView()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, OnCanvasPointerPressed, handledEventsToo: false);
        AddHandler(PointerMovedEvent, OnPointerMoved, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnPointerReleased, handledEventsToo: true);
        AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, handledEventsToo: true);
    }

    protected override void OnLoaded(global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        var canvas = this.FindControl<Control>("GraphCanvas");
        var viewport = this.FindControl<Control>("CanvasViewport");
        _linkPreviewLine = this.FindControl<Line>("LinkPreviewLine");
        if (canvas != null && viewport != null)
        {
            var tg = new TransformGroup();
            tg.Children.Add(_scaleTransform);
            tg.Children.Add(_translateTransform);
            canvas.RenderTransform = tg;
            _canvasPanel = canvas;
            _viewport = viewport;
            _viewport.SizeChanged += (_, _) => FitToView();
        }

        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainViewModel.CurrentProblem))
                    FitToView();
            };
        }
    }

    private void FitToView()
    {
        if (_canvasPanel == null || _viewport == null) return;
        if (DataContext is not MainViewModel vm) return;
        var agents = vm.CurrentProblem?.Agents;
        if (agents == null || agents.Count == 0) return;

        double areaW = _viewport.Bounds.Width;
        double areaH = _viewport.Bounds.Height;
        if (areaW <= 0 || areaH <= 0) return;

        const double pad = 30;
        double minX = agents.Min(a => a.X);
        double minY = agents.Min(a => a.Y);
        double maxX = agents.Max(a => a.X) + 140;
        double maxY = agents.Max(a => a.Y) + 64;

        double contentW = maxX - minX;
        double contentH = maxY - minY;

        // Scale to fit if content is bigger than area
        double scale = Math.Min(1.0, Math.Min((areaW - pad * 2) / contentW, (areaH - pad * 2) / contentH));
        scale = Math.Max(0.2, scale); // minimum scale

        _scaleTransform.ScaleX = scale;
        _scaleTransform.ScaleY = scale;

        // After scaling, center the content
        double scaledW = contentW * scale;
        double scaledH = contentH * scale;
        _currentPanX = (areaW - scaledW) / 2 - minX * scale;
        _currentPanY = (areaH - scaledH) / 2 - minY * scale;
        _translateTransform.X = _currentPanX;
        _translateTransform.Y = _currentPanY;

        // Update zoom display
        vm.Zoom = scale;
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(this).Properties;
        if (props.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _panStart = e.GetPosition(this);
            _panOffsetX = _currentPanX;
            _panOffsetY = _currentPanY;
            e.Pointer.Capture((Control)this);
            e.Handled = true;
        }
    }

    private void OnNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.DataContext is AgentViewModel a
            && DataContext is MainViewModel vm)
        {
            var props = e.GetCurrentPoint(c).Properties;
            // Don't start drag if right button (linking mode)
            if (props.IsRightButtonPressed) return;

            vm.SelectAgentCommand.Execute(a);
            Focus();

            _dragging = a;
            _dragStart = e.GetPosition(this);
            _startX = a.X;
            _startY = a.Y;
            e.Pointer.Capture(c);
            e.Handled = true;
        }
    }

    private void OnOutputConnectorPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && FindAgentFromConnector(c) is AgentViewModel agent
            && agent.Kind != AgentKind.Sink)
        {
            _isLinking = true;
            _linkSource = agent;
            if (_linkPreviewLine != null)
            {
                double startX = agent.X + Models.Agent.NodeWidth;
                double startY = agent.Y + Models.Agent.NodeHeight / 2;
                _linkPreviewLine.StartPoint = new Point(startX, startY);
                _linkPreviewLine.EndPoint = new Point(startX, startY);
                _linkPreviewLine.IsVisible = true;
            }
            e.Pointer.Capture((Control)this);
            e.Handled = true;
        }
    }

    private void OnInputConnectorPressed(object? sender, PointerPressedEventArgs e)
    {
        // Input connectors are targets — no action on press, 
        // but we could also start reverse linking here in the future.
        e.Handled = true;
    }

    private void OnEdgePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control c && c.DataContext is Edge edge
            && DataContext is MainViewModel vm)
        {
            vm.SelectEdgeCommand.Execute(edge);
            Focus();
            e.Handled = true;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete || DataContext is not MainViewModel vm) return;

        if (vm.SelectedEdge != null)
        {
            vm.DeleteSelectedEdgeCommand.Execute(null);
            e.Handled = true;
        }
        else if (vm.SelectedAgent != null)
        {
            vm.DeleteAgentCommand.Execute(vm.SelectedAgent);
            e.Handled = true;
        }
    }

    private AgentViewModel? FindAgentFromConnector(Control connector)
    {
        // Walk up visual tree to find the DataContext of the agent
        var current = connector as object;
        while (current is Control ctrl)
        {
            if (ctrl.DataContext is AgentViewModel a)
                return a;
            current = ctrl.Parent;
        }
        return null;
    }

    private AgentViewModel? HitTestAgent(Point canvasPoint)
    {
        if (DataContext is not MainViewModel vm) return null;
        foreach (var agent in vm.CurrentProblem.Agents)
        {
            if (canvasPoint.X >= agent.X && canvasPoint.X <= agent.X + Models.Agent.NodeWidth &&
                canvasPoint.Y >= agent.Y && canvasPoint.Y <= agent.Y + Models.Agent.NodeHeight)
            {
                return agent;
            }
        }
        return null;
    }

    private Point ScreenToCanvas(Point viewportPos)
    {
        double scale = _scaleTransform.ScaleX;
        return new Point(
            (viewportPos.X - _currentPanX) / scale,
            (viewportPos.Y - _currentPanY) / scale
        );
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(this);

        if (_isLinking && _linkPreviewLine != null && _viewport != null)
        {
            var vpPos = e.GetPosition(_viewport);
            var canvasPos = ScreenToCanvas(vpPos);
            _linkPreviewLine.EndPoint = canvasPos;
            return;
        }

        if (_isPanning)
        {
            _currentPanX = _panOffsetX + (pos.X - _panStart.X);
            _currentPanY = _panOffsetY + (pos.Y - _panStart.Y);
            _translateTransform.X = _currentPanX;
            _translateTransform.Y = _currentPanY;
            return;
        }

        if (_dragging == null) return;
        double scale = _scaleTransform.ScaleX;
        _dragging.X = _startX + (pos.X - _dragStart.X) / scale;
        _dragging.Y = _startY + (pos.Y - _dragStart.Y) / scale;

        if (DataContext is MainViewModel vm)
            vm.CurrentProblem?.Model.RebuildEdgeAnchors();
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (_canvasPanel == null || _viewport == null) return;

        const double zoomFactor = 1.15;
        const double minZoom = 0.1;
        const double maxZoom = 5.0;

        double oldScale = _scaleTransform.ScaleX;
        double newScale = e.Delta.Y > 0
            ? oldScale * zoomFactor
            : oldScale / zoomFactor;
        newScale = Math.Clamp(newScale, minZoom, maxZoom);

        // Zoom toward the mouse pointer position
        var mousePos = e.GetPosition(_viewport);
        double ratio = newScale / oldScale;

        _currentPanX = mousePos.X - (mousePos.X - _currentPanX) * ratio;
        _currentPanY = mousePos.Y - (mousePos.Y - _currentPanY) * ratio;

        _scaleTransform.ScaleX = newScale;
        _scaleTransform.ScaleY = newScale;
        _translateTransform.X = _currentPanX;
        _translateTransform.Y = _currentPanY;

        if (DataContext is MainViewModel vm)
            vm.Zoom = newScale;

        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
            return;
        }

        if (_isLinking && _linkSource != null)
        {
            _isLinking = false;
            if (_linkPreviewLine != null)
                _linkPreviewLine.IsVisible = false;

            var pos = _viewport != null ? e.GetPosition(_viewport) : e.GetPosition(this);
            var canvasPos = ScreenToCanvas(pos);
            var target = HitTestAgent(canvasPos);

            if (target != null && target != _linkSource && DataContext is MainViewModel vm)
            {
                vm.AddEdgeCommand.Execute($"{_linkSource.Id}|{target.Id}");
            }

            _linkSource = null;
            e.Pointer.Capture(null);
            return;
        }

        if (_dragging != null)
        {
            _dragging = null;
            e.Pointer.Capture(null);
        }
    }
}
