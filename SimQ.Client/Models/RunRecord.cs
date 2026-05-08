namespace SimQ.Client.Models;

public sealed class RunRecord
{
    public string Id { get; set; } = "";
    public string ProblemId { get; set; } = "";
    public string ProblemName { get; set; } = "";
    public RunStatus Status { get; set; }
    public string StartedAt { get; set; } = "";
    public string Duration { get; set; } = "";
    public int Iterations { get; set; }
    public string Note => Status switch
    {
        RunStatus.Done      => $"длительность {Duration}",
        RunStatus.Failed    => "не удалось запустить",
        RunStatus.Cancelled => "остановлено пользователем",
        RunStatus.Running   => "выполняется...",
        _ => "",
    };
}

public sealed class ResultRow
{
    public int N { get; set; }
    public double P { get; set; }
    public double Cdf { get; set; }
    /// <summary>Width in px for the inline distribution bar (max ≈ 220).</summary>
    public double BarWidth { get; set; }
    /// <summary>Height in px for the bar-chart column (max ≈ 200).</summary>
    public double ChartHeight { get; set; }

    /// <summary>P forced to scientific notation, e.g. "5.3132e-3".</summary>
    public string PScientific
        => P <= 0
            ? "0"
            : P.ToString("0.0000e+0", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>CDF formatted with 4 decimals.</summary>
    public string CdfFormatted
        => Cdf.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);
}
