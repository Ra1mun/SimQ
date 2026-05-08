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
    public class StatisticCollectorTests
    {
        [Fact]
        public void CollectStatistic_WithEmptyData_DoesNotThrow()
        {
            var problem = new Problem { Name = "Test", Agents = new List<IModellingAgent>() };
            var dataCollector = new DataCollector(problem);
            var statisticCollector = new StatisticCollector();

            var exception = Record.Exception(() => statisticCollector.CollectStatistic(dataCollector));
            Assert.Null(exception);
        }

        [Fact]
        public void CollectStatistic_WithData_PopulatesAverageAndStates()
        {
            var mock = new Mock<IModellingAgent>();
            mock.Setup(a => a.Id).Returns("a1");
            mock.As<IAgentStatistic>().Setup(s => s.GetCurrentState()).Returns(1);

            var problem = new Problem { Name = "Test", Agents = new List<IModellingAgent>() };
            problem.AddAgentForStatistic(mock.Object);
            var dc = new DataCollector(problem);

            dc.AddState(5.0, new List<IModellingAgent> { mock.Object });
            dc.AddState(5.0, new List<IModellingAgent> { mock.Object });

            var sc = new StatisticCollector();
            sc.CollectStatistic(dc);

            Assert.NotNull(sc.average);
            Assert.NotNull(sc.states);
            Assert.True(sc.average.ContainsKey(mock.Object));
        }
    }
}
