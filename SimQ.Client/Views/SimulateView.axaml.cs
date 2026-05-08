using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using SimQ.Client.ViewModels;

namespace SimQ.Client.Views;

public partial class SimulateView : UserControl
{
    // Pan state
    private bool _isPanning;
    private Point _panStart;
    private double _panOffsetX, _panOffsetY;
    private double _currentPanX, _currentPanY;

    private ScaleTransform _scaleTransform = new(1, 1);
    private TranslateTransform _translateTransform = new();
    private Control? _canvasPanel;
    private Control? _viewport;

    public SimulateView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        _canvasPanel = this.FindControl<Control>("SimGraphCanvas");
        _viewport = this.FindControl<Control>("SimCanvasViewport");
        if (_canvasPanel != null && _viewport != null)
        {
            var tg = new TransformGroup();
            tg.Children.Add(_scaleTransform);
            tg.Children.Add(_translateTransform);
            _canvasPanel.RenderTransform = tg;
            _viewport.SizeChanged += (_, _) => FitToView();

            // Attach pan/zoom handlers only to the canvas viewport
            _viewport.AddHandler(PointerPressedEvent, OnCanvasPointerPressed, handledEventsToo: false);
            _viewport.AddHandler(PointerMovedEvent, OnPointerMoved, handledEventsToo: true);
            _viewport.AddHandler(PointerReleasedEvent, OnPointerReleased, handledEventsToo: true);
            _viewport.AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, handledEventsToo: true);
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

        double scale = Math.Min(1.0, Math.Min((areaW - pad * 2) / contentW, (areaH - pad * 2) / contentH));
        scale = Math.Max(0.2, scale);

        _scaleTransform.ScaleX = scale;
        _scaleTransform.ScaleY = scale;

        double scaledW = contentW * scale;
        double scaledH = contentH * scale;
        _currentPanX = (areaW - scaledW) / 2 - minX * scale;
        _currentPanY = (areaH - scaledH) / 2 - minY * scale;
        _translateTransform.X = _currentPanX;
        _translateTransform.Y = _currentPanY;
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_viewport == null) return;
        var props = e.GetCurrentPoint(_viewport).Properties;
        // Pan with middle button OR left button (read-only canvas)
        if (props.IsMiddleButtonPressed || props.IsLeftButtonPressed)
        {
            _isPanning = true;
            _panStart = e.GetPosition(_viewport);
            _panOffsetX = _currentPanX;
            _panOffsetY = _currentPanY;
            e.Pointer.Capture((IInputElement)_viewport);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPanning || _viewport == null) return;
        var pos = e.GetPosition(_viewport);
        _currentPanX = _panOffsetX + (pos.X - _panStart.X);
        _currentPanY = _panOffsetY + (pos.Y - _panStart.Y);
        _translateTransform.X = _currentPanX;
        _translateTransform.Y = _currentPanY;
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

        var mousePos = e.GetPosition(_viewport);
        double ratio = newScale / oldScale;

        _currentPanX = mousePos.X - (mousePos.X - _currentPanX) * ratio;
        _currentPanY = mousePos.Y - (mousePos.Y - _currentPanY) * ratio;

        _scaleTransform.ScaleX = newScale;
        _scaleTransform.ScaleY = newScale;
        _translateTransform.X = _currentPanX;
        _translateTransform.Y = _currentPanY;

        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            e.Pointer.Capture(null);
        }
    }
}
