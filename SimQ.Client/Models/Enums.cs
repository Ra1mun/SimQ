namespace SimQ.Client.Models;

public enum AgentKind { Source, ServiceBlock, Buffer, Orbit, Sink }

public enum ProblemStatus { Draft, Ready, Running, Failed }

public enum RunStatus { Done, Failed, Cancelled, Running }

public enum DistributionKind
{
    Exponential,
    Normal,
    Bernoulli,
    Beta,
    Binomial,
    Poisson,
    Gamma,
    Rayleigh,
    Geometric,
    Pascal,
    Hypergeometric,
    F,
    T,
}
