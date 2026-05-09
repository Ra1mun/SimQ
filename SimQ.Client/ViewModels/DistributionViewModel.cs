using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using SimQ.Client.Models;

namespace SimQ.Client.ViewModels;

public partial class DistributionViewModel : ObservableObject
{
    public DistributionParams Model { get; }
    private readonly Action? _onChanged;

    public DistributionViewModel(DistributionParams model, Action? onChanged = null)
    {
        Model = model;
        _onChanged = onChanged;
        _kind  = model.Kind;
        _rate  = model.Rate;
        _p     = model.P;
        _a     = model.A;
        _b     = model.B;
        _mean  = model.Mean;
        _std   = model.Std;
        _k     = model.K;
        _theta = model.Theta;
        _n     = model.N;
        _r     = model.R;
        _bigN  = model.BigN;
        _bigK  = model.BigK;
    }

    public IReadOnlyList<DistributionKind> AllKinds => DistributionParams.All;

    [ObservableProperty] private DistributionKind _kind;
    partial void OnKindChanged(DistributionKind value)
    {
        Model.Kind = value;
        RaiseAllVisibility();
        OnPropertyChanged(nameof(KindLabel));
        OnPropertyChanged(nameof(Summary));
        _onChanged?.Invoke();
    }

    [ObservableProperty] private double _rate;
    partial void OnRateChanged(double value)  { Model.Rate  = value; Notify(); }

    [ObservableProperty] private double _p;
    partial void OnPChanged(double value)     { Model.P     = value; Notify(); }

    [ObservableProperty] private double _a;
    partial void OnAChanged(double value)     { Model.A     = value; Notify(); }

    [ObservableProperty] private double _b;
    partial void OnBChanged(double value)     { Model.B     = value; Notify(); }

    [ObservableProperty] private double _mean;
    partial void OnMeanChanged(double value)  { Model.Mean  = value; Notify(); }

    [ObservableProperty] private double _std;
    partial void OnStdChanged(double value)   { Model.Std   = value; Notify(); }

    [ObservableProperty] private double _k;
    partial void OnKChanged(double value)     { Model.K     = value; Notify(); }

    [ObservableProperty] private double _theta;
    partial void OnThetaChanged(double value) { Model.Theta = value; Notify(); }

    [ObservableProperty] private int _n;
    partial void OnNChanged(int value)        { Model.N     = value; Notify(); }

    [ObservableProperty] private int _r;
    partial void OnRChanged(int value)        { Model.R     = value; Notify(); }

    [ObservableProperty] private int _bigN;
    partial void OnBigNChanged(int value)     { Model.BigN  = value; Notify(); }

    [ObservableProperty] private int _bigK;
    partial void OnBigKChanged(int value)     { Model.BigK  = value; Notify(); }

    private void Notify() { OnPropertyChanged(nameof(Summary)); _onChanged?.Invoke(); }

    // Visibility flags — one per distribution kind
    public bool IsExponential    => Kind == DistributionKind.Exponential;
    public bool IsNormal         => Kind == DistributionKind.Normal;
    public bool IsBernoulli      => Kind == DistributionKind.Bernoulli;
    public bool IsBeta           => Kind == DistributionKind.Beta;
    public bool IsBinomial       => Kind == DistributionKind.Binomial;
    public bool IsPoisson        => Kind == DistributionKind.Poisson;
    public bool IsGamma          => Kind == DistributionKind.Gamma;
    public bool IsRayleigh       => Kind == DistributionKind.Rayleigh;
    public bool IsGeometric      => Kind == DistributionKind.Geometric;
    public bool IsPascal         => Kind == DistributionKind.Pascal;
    public bool IsHypergeometric => Kind == DistributionKind.Hypergeometric;
    public bool IsF              => Kind == DistributionKind.F;
    public bool IsT              => Kind == DistributionKind.T;

    private void RaiseAllVisibility()
    {
        OnPropertyChanged(nameof(IsExponential));
        OnPropertyChanged(nameof(IsNormal));
        OnPropertyChanged(nameof(IsBernoulli));
        OnPropertyChanged(nameof(IsBeta));
        OnPropertyChanged(nameof(IsBinomial));
        OnPropertyChanged(nameof(IsPoisson));
        OnPropertyChanged(nameof(IsGamma));
        OnPropertyChanged(nameof(IsRayleigh));
        OnPropertyChanged(nameof(IsGeometric));
        OnPropertyChanged(nameof(IsPascal));
        OnPropertyChanged(nameof(IsHypergeometric));
        OnPropertyChanged(nameof(IsF));
        OnPropertyChanged(nameof(IsT));
    }

    public string KindLabel => Kind switch
    {
        DistributionKind.Exponential     => "Экспоненциальное",
        DistributionKind.Normal          => "Нормальное",
        DistributionKind.Bernoulli       => "Бернулли",
        DistributionKind.Beta            => "Бета",
        DistributionKind.Binomial        => "Биномиальное",
        DistributionKind.Poisson         => "Пуассона",
        DistributionKind.Gamma           => "Гамма",
        DistributionKind.Rayleigh        => "Рэлея",
        DistributionKind.Geometric       => "Геометрическое",
        DistributionKind.Pascal          => "Паскаля",
        DistributionKind.Hypergeometric  => "Гипергеометрическое",
        DistributionKind.F               => "Фишера (F)",
        DistributionKind.T               => "Стьюдента (t)",
        _ => Kind.ToString(),
    };

    public string Summary => Model.Format();
}
