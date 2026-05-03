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
}
