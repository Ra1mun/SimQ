using Avalonia.Media;

namespace SimQ.Client.Models;

public enum SimLogLevel { Info, Debug, Warn, Error, Ok }

public sealed class SimLogEntry
{
    public string Timestamp { get; set; } = "";
    public SimLogLevel Level { get; set; } = SimLogLevel.Info;
    public string Source { get; set; } = "";
    public string Message { get; set; } = "";

    public string LevelLabel => Level switch
    {
        SimLogLevel.Info  => "INFO ",
        SimLogLevel.Debug => "DEBUG",
        SimLogLevel.Warn  => "WARN ",
        SimLogLevel.Error => "ERROR",
        SimLogLevel.Ok    => "OK   ",
        _ => "INFO ",
    };

    public IBrush LevelBrush => Level switch
    {
        SimLogLevel.Info  => new SolidColorBrush(Color.Parse("#6CB6FF")),
        SimLogLevel.Debug => new SolidColorBrush(Color.Parse("#9AA4B2")),
        SimLogLevel.Warn  => new SolidColorBrush(Color.Parse("#E0C36B")),
        SimLogLevel.Error => new SolidColorBrush(Color.Parse("#F08784")),
        SimLogLevel.Ok    => new SolidColorBrush(Color.Parse("#7BD389")),
        _ => new SolidColorBrush(Color.Parse("#6CB6FF")),
    };

    public IBrush TimeBrush { get; } = new SolidColorBrush(Color.Parse("#88AABB"));
    public IBrush SourceBrush { get; } = new SolidColorBrush(Color.Parse("#B8C4D6"));
}
