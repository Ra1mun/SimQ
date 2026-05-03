using System.Collections.Generic;

namespace SimQ.Client.Models;

/// <summary>
/// Distribution parameters for an agent. Maps to SimQ.Core's distribution model.
/// Different distributions use a subset of these fields:
///   M (exponential)        — Rate
///   D (deterministic)      — Value
///   Bernoulli              — P
///   Beta                   — A, B
///   G (general)            — Mean, Std
/// </summary>
public sealed class DistributionParams
{
    public DistributionKind Kind { get; set; } = DistributionKind.M;
    public double Rate  { get; set; } = 0.3;   // λ
    public double Value { get; set; } = 1.0;   // t
    public double P     { get; set; } = 0.2;
    public double A     { get; set; } = 0.2;   // α
    public double B     { get; set; } = 0.3;   // β
    public double Mean  { get; set; } = 1.0;
    public double Std   { get; set; } = 0.5;

    public string Format() => Kind switch
    {
        DistributionKind.M         => $"M  λ={Rate}",
        DistributionKind.D         => $"D  t={Value}",
        DistributionKind.Bernoulli => $"Bern  p={P}",
        DistributionKind.Beta      => $"Beta  α={A} β={B}",
        DistributionKind.G         => $"G  μ={Mean} σ={Std}",
        _ => Kind.ToString(),
    };

    public static IReadOnlyList<DistributionKind> All { get; } = new[]
    {
        DistributionKind.M,
        DistributionKind.D,
        DistributionKind.Bernoulli,
        DistributionKind.Beta,
        DistributionKind.G,
    };
}
