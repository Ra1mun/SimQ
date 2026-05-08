using Xunit;
using SimQ.Core.Models.Distributions;

namespace SimQ.Tests.Distributions
{
    public class ExponentialDistributionTests
    {
        [Fact]
        public void Generate_ReturnsPositiveValue()
        {
            var dist = new ExponentialDistribution(1.0);
            var value = dist.Generate();
            Assert.True(value >= 0, $"Expected non-negative, got {value}");
        }

        [Fact]
        public void Generate_WithHigherRate_ProducesSmallerMean()
        {
            var distLow = new ExponentialDistribution(0.5);
            var distHigh = new ExponentialDistribution(5.0);

            double sumLow = 0, sumHigh = 0;
            int n = 10000;
            for (int i = 0; i < n; i++)
            {
                sumLow += distLow.Generate();
                sumHigh += distHigh.Generate();
            }

            Assert.True(sumLow / n > sumHigh / n,
                "Lower rate should produce larger mean values");
        }
    }

    public class NormalDistributionTests
    {
        [Fact]
        public void Generate_ProducesValues()
        {
            var dist = new NormalDistribution(0, 1);
            var value = dist.Generate();
            Assert.False(double.IsNaN(value));
        }

        [Fact]
        public void Generate_MeanApproximatesExpected()
        {
            var mu = 5.0;
            var dist = new NormalDistribution(mu, 1.0);

            double sum = 0;
            int n = 10000;
            for (int i = 0; i < n; i++)
                sum += dist.Generate();

            var mean = sum / n;
            Assert.True(Math.Abs(mean - mu) < 0.5,
                $"Mean {mean} should be close to {mu}");
        }
    }

    public class BernoulliDistributionTests
    {
        [Fact]
        public void Generate_ReturnsZeroOrOne()
        {
            var dist = new BernoulliDistribution(0.5);
            for (int i = 0; i < 100; i++)
            {
                var value = dist.Generate();
                Assert.True(value == 0 || value == 1, $"Got unexpected value: {value}");
            }
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(1.0)]
        public void Generate_WithExtremeProbability_ProducesCorrectValues(double p)
        {
            var dist = new BernoulliDistribution(p);
            double sum = 0;
            int n = 100;
            for (int i = 0; i < n; i++)
                sum += dist.Generate();

            if (p == 0.0)
                Assert.Equal(0, sum);
            else
                Assert.Equal(n, sum);
        }
    }

    public class BinomialDistributionTests
    {
        [Fact]
        public void Generate_ReturnsNonNegativeValue()
        {
            var dist = new BinomialDistribution(0.5, 10);
            for (int i = 0; i < 100; i++)
            {
                var value = dist.Generate();
                Assert.True(value >= 0, $"Expected non-negative, got {value}");
            }
        }

        [Fact]
        public void Generate_DoesNotExceedN()
        {
            int n = 10;
            var dist = new BinomialDistribution(0.5, n);
            for (int i = 0; i < 100; i++)
            {
                var value = dist.Generate();
                Assert.True(value <= n, $"Value {value} exceeds n={n}");
            }
        }
    }

    public class RayleighDistributionTests
    {
        [Fact]
        public void Generate_ReturnsPositiveValue()
        {
            var dist = new RayleighDistribution(1.0);
            for (int i = 0; i < 100; i++)
            {
                var value = dist.Generate();
                Assert.True(value >= 0, $"Expected non-negative, got {value}");
            }
        }
    }

    public class GammaDistributionTests
    {
        [Fact]
        public void Generate_ReturnsPositiveValue()
        {
            var dist = new GammaDistribution(2.0, 1.0);
            for (int i = 0; i < 100; i++)
            {
                var value = dist.Generate();
                Assert.True(value >= 0, $"Expected non-negative, got {value}");
            }
        }
    }
}
