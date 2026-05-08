using Xunit;
using SimQ.Core.Models.Base;
using Moq;
using Problem = SimQ.Core.Models.Problem;

namespace SimQ.Tests.Models
{
    public class ProblemTests
    {
        [Fact]
        public void Problem_DefaultValues_AreCorrect()
        {
            var problem = new Problem();

            Assert.Equal(30 * 60, problem.MaxRealTime);
            Assert.Equal(1_000_000, problem.MaxEventsAmount);
            Assert.Equal(1_000, problem.MaxModelationTime);
        }

        [Fact]
        public void AddAgentForStatistic_AddsAgent()
        {
            var problem = new Problem();
            var agent = new Mock<IModellingAgent>();
            agent.Setup(a => a.Id).Returns("a1");

            problem.AddAgentForStatistic(agent.Object);

            Assert.Single(problem.AgentsForStatistic);
            Assert.Equal(agent.Object, problem.AgentsForStatistic[0]);
        }

        [Fact]
        public void AddAgentForStatistic_MultipleAgents()
        {
            var problem = new Problem();
            var a1 = new Mock<IModellingAgent>().Object;
            var a2 = new Mock<IModellingAgent>().Object;

            problem.AddAgentForStatistic(a1);
            problem.AddAgentForStatistic(a2);

            Assert.Equal(2, problem.AgentsForStatistic.Count);
        }
    }

    public class GenerationErrorSettingsTests
    {
        [Fact]
        public void DefaultValues_AreCorrect()
        {
            var settings = new SimQ.Core.Models.GenerationErrorSettings();

            Assert.Equal(10_000, settings.GenerationErrorCheckStep);
            Assert.Equal(3, settings.GenerationErrorCheckStepModifier);
            Assert.Equal(0.00001, settings.MinGenerationError);
        }
    }
}
