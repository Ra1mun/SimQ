using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
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

    private ScaleTransform _scaleTransform = new(1, 1);
    private TranslateTransform _translateTransform = new();
    private Panel? _canvasPanel;

    public EditorView()
    {
        InitializeComponent();
        AddHandler(PointerPressedEvent, OnCanvasPointerPressed, handledEventsToo: false);
        AddHandler(PointerMovedEvent, OnPointerMoved, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnPointerReleased, handledEventsToo: true);
    }

    protected override void OnLoaded(global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        var canvas = this.FindControl<Panel>("GraphCanvas");
        if (canvas != null)
        {
            var tg = new TransformGroup();
            tg.Children.Add(_scaleTransform);
            tg.Children.Add(_translateTransform);
            canvas.RenderTransform = tg;
            _canvasPanel = canvas;
            canvas.SizeChanged += (_, _) => FitToView();
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
        if (_canvasPanel == null) return;
        if (DataContext is not MainViewModel vm) return;
        var agents = vm.CurrentProblem?.Agents;
        if (agents == null || agents.Count == 0) return;

        double areaW = _canvasPanel.Bounds.Width;
        double areaH = _canvasPanel.Bounds.Height;
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
            vm.SelectAgentCommand.Execute(a);

            _dragging = a;
            _dragStart = e.GetPosition(this);
            _startX = a.X;
            _startY = a.Y;
            e.Pointer.Capture(c);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(this);

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

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
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
