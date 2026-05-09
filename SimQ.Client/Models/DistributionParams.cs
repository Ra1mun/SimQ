using System.Collections.Generic;

namespace SimQ.Client.Models;

/// <summary>
/// Distribution parameters for an agent. Maps to SimQ.Core's distribution model.
/// Each distribution uses a subset of these fields; the UI shows only the relevant ones.
/// </summary>
public sealed class DistributionParams
{
    public DistributionKind Kind { get; set; } = DistributionKind.Exponential;

    // Shared / multi-use parameters
    public double Rate   { get; set; } = 0.3;   // lambda  (Exponential, Poisson)
    public double P      { get; set; } = 0.5;   // probability (Bernoulli, Binomial, Geometric, Pascal)
    public double A      { get; set; } = 2.0;   // alpha / a / df1 (Beta, F, T, Hypergeometric-N)
    public double B      { get; set; } = 5.0;   // beta  / b / df2 (Beta, F, Hypergeometric-n)
    public double Mean   { get; set; } = 0.0;   // mu  (Normal)
    public double Std    { get; set; } = 1.0;   // sigma (Normal, Rayleigh)
    public double K      { get; set; } = 2.0;   // shape (Gamma)
    public double Theta  { get; set; } = 1.0;   // scale (Gamma)
    public int    N      { get; set; } = 10;     // n (Binomial, Pascal-r, Hypergeometric-N/n/K)
    public int    R      { get; set; } = 3;      // r (Pascal)
    public int    BigN   { get; set; } = 50;     // N  (Hypergeometric)
    public int    BigK   { get; set; } = 20;     // K  (Hypergeometric)

    public string Format() => Kind switch
    {
        DistributionKind.Exponential     => $"Эксп  λ={Rate}",
        DistributionKind.Normal          => $"Норм  μ={Mean} σ={Std}",
        DistributionKind.Bernoulli       => $"Берн  p={P}",
        DistributionKind.Beta            => $"Бета  α={A} β={B}",
        DistributionKind.Binomial        => $"Бином  p={P} n={N}",
        DistributionKind.Poisson         => $"Пуасс  λ={Rate}",
        DistributionKind.Gamma           => $"Гамма  k={K} θ={Theta}",
        DistributionKind.Rayleigh        => $"Рэлей  σ={Std}",
        DistributionKind.Geometric       => $"Геом  p={P}",
        DistributionKind.Pascal          => $"Паск  p={P} r={R}",
        DistributionKind.Hypergeometric  => $"Гипергеом  N={BigN} n={N} K={BigK}",
        DistributionKind.F               => $"F  d₁={A} d₂={B}",
        DistributionKind.T               => $"t  ν={A}",
        _ => Kind.ToString(),
    };

    public static IReadOnlyList<DistributionKind> All { get; } = new[]
    {
        DistributionKind.Exponential,
        DistributionKind.Normal,
        DistributionKind.Bernoulli,
        DistributionKind.Beta,
        DistributionKind.Binomial,
        DistributionKind.Poisson,
        DistributionKind.Gamma,
        DistributionKind.Rayleigh,
        DistributionKind.Geometric,
        DistributionKind.Pascal,
        DistributionKind.Hypergeometric,
        DistributionKind.F,
        DistributionKind.T,
    };
}
