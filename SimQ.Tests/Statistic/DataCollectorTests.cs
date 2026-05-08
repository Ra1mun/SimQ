using System;
using Xunit;
using Moq;
using SimQ.Core.Statistic;
using SimQ.Core.Models;
using SimQ.Core.Models.Base;
using SimQ.Domain.Models.ProblemAggregation;
using System.Collections.Generic;
using Problem = SimQ.Core.Models.Problem;

namespace SimQ.Tests.Statistic
{
    public class DataCollectorTests
    {
        private Problem CreateProblem(List<IModellingAgent>? agentsForStatistic = null)
        {
            var problem = new Problem
            {
                Name = "TestProblem",
                Agents = new List<IModellingAgent>(),
                MaxEventsAmount = 100,
                MaxModelationTime = 100,
                MaxRealTime = 60,
                GenerationErrorSettings = new GenerationErrorSettings()
            };
            if (agentsForStatistic != null)
                foreach (var a in agentsForStatistic) problem.AddAgentForStatistic(a);
            return problem;
        }

        private Mock<IModellingAgent> CreateMockAgent(string id = "agent1")
        {
            var mock = new Mock<IModellingAgent>();
            mock.Setup(a => a.Id).Returns(id);
            mock.Setup(a => a.Type).Returns(AgentType.SOURCE);
            return mock;
        }

        [Fact]
        public void Constructor_InitializesEmptyStatisticData()
        {
            var problem = CreateProblem();
            var collector = new DataCollector(problem);

            Assert.NotNull(collector.agentsStatisticData);
            Assert.Empty(collector.agentsStatisticData);
            Assert.Equal(0, collector.CurrentEventsAmount);
            Assert.Equal(0, collector.CurrentModelationTime);
            Assert.Equal(1, collector.CurrentGenerationError);
        }

        [Fact]
        public void Constructor_WithAgentsForStatistic_AddsToData()
        {
            var agent = CreateMockAgent();
            var problem = CreateProblem(new List<IModellingAgent> { agent.Object });

            var collector = new DataCollector(problem);

            Assert.Single(collector.agentsStatisticData);
            Assert.True(collector.agentsStatisticData.ContainsKey(agent.Object));
        }

        [Fact]
        public void AddState_IncrementsEventsAndTime()
        {
            var agentMock = CreateMockAgent();
            var agentStatMock = new Mock<IModellingAgent>();
            // We need an agent that implements both IModellingAgent and IAgentStatistic
            // Use a combined mock
            var combinedMock = new Mock<IModellingAgent>();
            combinedMock.Setup(a => a.Id).Returns("a1");
            combinedMock.As<IAgentStatistic>().Setup(s => s.GetCurrentState()).Returns(0);

            var problem = CreateProblem(new List<IModellingAgent> { combinedMock.Object });
            var collector = new DataCollector(problem);

            collector.AddState(1.5, new List<IModellingAgent> { combinedMock.Object });

            Assert.Equal(1.5, collector.CurrentModelationTime);
            Assert.Equal(1, collector.CurrentEventsAmount);
        }

        [Fact]
        public void AddState_AccumulatesStateTime()
        {
            var combinedMock = new Mock<IModellingAgent>();
            combinedMock.Setup(a => a.Id).Returns("a1");
            combinedMock.As<IAgentStatistic>().Setup(s => s.GetCurrentState()).Returns(2);

            var problem = CreateProblem(new List<IModellingAgent> { combinedMock.Object });
            var collector = new DataCollector(problem);

            collector.AddState(1.0, new List<IModellingAgent> { combinedMock.Object });
            collector.AddState(2.0, new List<IModellingAgent> { combinedMock.Object });

            Assert.Equal(3.0, collector.CurrentModelationTime);
            Assert.Equal(2, collector.CurrentEventsAmount);
            // State 2 should have 3.0 total time
            Assert.Equal(3.0, collector.agentsStatisticData[combinedMock.Object][2]);
        }

        [Fact]
        public void IsDone_ReturnsFalse_WhenBelowLimits()
        {
            var problem = CreateProblem();
            var collector = new DataCollector(problem);

            Assert.False(collector.isDone);
        }

        [Fact]
        public void IsDone_ReturnsTrue_WhenEventsExceedMax()
        {
            var problem = CreateProblem();
            problem.MaxEventsAmount = 5;
            var collector = new DataCollector(problem);

            for (int i = 0; i < 6; i++)
                collector.AddState(0.1, new List<IModellingAgent>());

            Assert.True(collector.isDone);
        }

        [Fact]
        public void IsDone_ReturnsTrue_WhenModelTimeExceedsMax()
        {
            var problem = CreateProblem();
            problem.MaxModelationTime = 10;
            var collector = new DataCollector(problem);

            collector.AddState(11, new List<IModellingAgent>());

            Assert.True(collector.isDone);
        }

        [Fact]
        public void BuildResult_ReturnsCorrectStructure()
        {
            var combinedMock = new Mock<IModellingAgent>();
            combinedMock.Setup(a => a.Id).Returns("a1");
            combinedMock.Setup(a => a.Type).Returns(AgentType.BUFFER);
            combinedMock.As<IAgentStatistic>().Setup(s => s.GetCurrentState()).Returns(1);

            var problem = CreateProblem(new List<IModellingAgent> { combinedMock.Object });
            var collector = new DataCollector(problem);

            collector.AddState(5.0, new List<IModellingAgent> { combinedMock.Object });
            collector.AddState(5.0, new List<IModellingAgent> { combinedMock.Object });

            var result = collector.BuildResult(2.5);

            Assert.Equal(2.5, result.EndRealTime);
            Assert.Equal(10.0, result.CurrentModelationTime);
            Assert.Equal(2, result.CurrentEventsAmount);
            Assert.Single(result.AgentResults);
            Assert.Equal("a1", result.AgentResults[0].AgentId);
        }

        [Fact]
        public void BuildResult_CalculatesCorrectProbabilities()
        {
            var mock1 = new Mock<IModellingAgent>();
            mock1.Setup(a => a.Id).Returns("a1");
            mock1.Setup(a => a.Type).Returns(AgentType.BUFFER);

            int callCount = 0;
            mock1.As<IAgentStatistic>().Setup(s => s.GetCurrentState())
                .Returns(() => callCount++ < 2 ? 0 : 1);

            var problem = CreateProblem(new List<IModellingAgent> { mock1.Object });
            var collector = new DataCollector(problem);

            // State 0 for 2 events, state 1 for 2 events, each deltaT = 1.0
            for (int i = 0; i < 4; i++)
                collector.AddState(1.0, new List<IModellingAgent> { mock1.Object });

            var result = collector.BuildResult(1.0);

            // TotalTime = 4.0; state 0 = 2.0 time, state 1 = 2.0 time
            Assert.Equal(0.5, result.AgentResults[0].StatesProbabilities["0"]);
            Assert.Equal(0.5, result.AgentResults[0].StatesProbabilities["1"]);
        }
    }
}
