using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SimQ.Client.Models;

namespace SimQ.Client.Converters;

/// <summary>
/// Converts an <see cref="AgentKind"/> to a swatch <see cref="IBrush"/>.
/// Uses the same palette as the HTML prototype.
/// </summary>
public sealed class AgentKindToBrushConverter : IValueConverter
{
    public static readonly AgentKindToBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AgentKind kind)
        {
            return kind switch
            {
                AgentKind.Source       => new SolidColorBrush(Color.Parse("#C99A2E")),
                AgentKind.ServiceBlock => new SolidColorBrush(Color.Parse("#3D5FCC")),
                AgentKind.Buffer       => new SolidColorBrush(Color.Parse("#3F9C68")),
                AgentKind.Orbit        => new SolidColorBrush(Color.Parse("#9447A6")),
                AgentKind.Sink         => new SolidColorBrush(Color.Parse("#7A7E89")),
                _                      => Brushes.Gray,
            };
        }
        return Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class RunStatusToBrushConverter : IValueConverter
{
    public static readonly RunStatusToBrushConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RunStatus s)
        {
            return s switch
            {
                RunStatus.Done      => new SolidColorBrush(Color.Parse("#2D8A4F")),
                RunStatus.Failed    => new SolidColorBrush(Color.Parse("#C53030")),
                RunStatus.Cancelled => new SolidColorBrush(Color.Parse("#828998")),
                RunStatus.Running   => new SolidColorBrush(Color.Parse("#3D5FCC")),
                _                   => Brushes.Gray,
            };
        }
        return Brushes.Gray;
    }
    public object? ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class RunStatusToLabelConverter : IValueConverter
{
    public static readonly RunStatusToLabelConverter Instance = new();
    public object? Convert(object? value, Type t, object? p, CultureInfo c) => value is RunStatus s ? s switch
    {
        RunStatus.Done      => "завершён",
        RunStatus.Failed    => "ошибка",
        RunStatus.Cancelled => "отменён",
        RunStatus.Running   => "выполняется",
        _                   => s.ToString(),
    } : "—";
    public object? ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class ProblemStatusToLabelConverter : IValueConverter
{
    public static readonly ProblemStatusToLabelConverter Instance = new();
    public object? Convert(object? value, Type t, object? p, CultureInfo c) => value is ProblemStatus s ? s switch
    {
        ProblemStatus.Draft   => "черновик",
        ProblemStatus.Ready   => "готово",
        ProblemStatus.Running => "идёт",
        ProblemStatus.Failed  => "ошибка",
        _                     => s.ToString(),
    } : "—";
    public object? ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class ProblemStatusToBrushConverter : IValueConverter
{
    public static readonly ProblemStatusToBrushConverter Instance = new();
    public object? Convert(object? value, Type t, object? p, CultureInfo c) => value is ProblemStatus s ? s switch
    {
        ProblemStatus.Draft   => new SolidColorBrush(Color.Parse("#B07A1A")),
        ProblemStatus.Ready   => new SolidColorBrush(Color.Parse("#2D8A4F")),
        ProblemStatus.Running => new SolidColorBrush(Color.Parse("#3D5FCC")),
        ProblemStatus.Failed  => new SolidColorBrush(Color.Parse("#C53030")),
        _ => (object)Brushes.Gray,
    } : Brushes.Gray;
    public object? ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class ProgressToWidthConverter : IValueConverter
{
    public static readonly ProgressToWidthConverter Instance = new();
    public object? Convert(object? value, Type t, object? p, CultureInfo c)
    {
        var max = double.TryParse(p?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var m) ? m : 600;
        var v = value is double d ? d : 0;
        return Math.Max(0, Math.Min(1, v)) * max;
    }
    public object? ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

public sealed class BoolToOpacityConverter : IValueConverter
{
    public static readonly BoolToOpacityConverter Instance = new();
    public object? Convert(object? value, Type t, object? p, CultureInfo c) => value is bool b && b ? 1.0 : 0.0;
    public object? ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}

/// <summary>
/// Bool → green/red brush, used for the "API online" status indicator dot.
/// </summary>
public sealed class BoolToOnlineBrushConverter : IValueConverter
{
    public static readonly BoolToOnlineBrushConverter Instance = new();
    public object? Convert(object? value, Type t, object? p, CultureInfo c)
        => value is bool b && b
            ? new SolidColorBrush(Color.Parse("#2D8A4F"))
            : new SolidColorBrush(Color.Parse("#C53030"));
    public object? ConvertBack(object? value, Type t, object? p, CultureInfo c) => throw new NotSupportedException();
}
