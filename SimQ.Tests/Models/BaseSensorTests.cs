using Xunit;
using SimQ.Core.Models.Base;

namespace SimQ.Tests.Models
{
    public class BaseSensorTests
    {
        [Fact]
        public void Next_ReturnsValueInDefaultRange()
        {
            var sensor = new BaseSensor(seed: 42);
            for (int i = 0; i < 1000; i++)
            {
                var value = sensor.Next();
                Assert.InRange(value, 0.0, 1.0);
            }
        }

        [Fact]
        public void Next_WithCustomRange_ReturnsValueInRange()
        {
            var sensor = new BaseSensor(seed: 42, a: 5.0, b: 10.0);
            for (int i = 0; i < 1000; i++)
            {
                var value = sensor.Next();
                Assert.InRange(value, 5.0, 10.0);
            }
        }

        [Fact]
        public void Next_WithSameSeed_ProducesSameSequence()
        {
            var s1 = new BaseSensor(seed: 123);
            var s2 = new BaseSensor(seed: 123);

            for (int i = 0; i < 100; i++)
                Assert.Equal(s1.Next(), s2.Next());
        }

        [Fact]
        public void Next_WithDifferentSeeds_ProducesDifferentSequences()
        {
            var s1 = new BaseSensor(seed: 1);
            var s2 = new BaseSensor(seed: 2);

            bool anyDifferent = false;
            for (int i = 0; i < 100; i++)
            {
                if (s1.Next() != s2.Next())
                {
                    anyDifferent = true;
                    break;
                }
            }
            Assert.True(anyDifferent);
        }

        [Fact]
        public void Next_CustomAB_ReturnsValueInRange()
        {
            var sensor = new BaseSensor(seed: 42);
            for (int i = 0; i < 100; i++)
            {
                var value = sensor.Next(2.0, 8.0);
                Assert.InRange(value, 2.0, 8.0);
            }
        }
    }
}
