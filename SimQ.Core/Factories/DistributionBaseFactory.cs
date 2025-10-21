using SimQCore.Library.Distributions;

namespace SimQ.Core.Factories;

public class DistributionBaseFactory : BaseFactory<IDistribution>
{
    protected override IDistribution CreateAgent(string reflectionType, params object?[] args)
    {
        throw new NotImplementedException();
    }
}