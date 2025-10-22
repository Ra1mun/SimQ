using SimQ.Core.Statistic;
using SimQCore;
using SimQCore.Library.Distributions;
using SimQCore.Modeller;
using SimQCore.Modeller.Models;
using SimQCore.Modeller.Models.Common;
using SimQCore.Modeller.Models.UserModels;

namespace SimQ.Simulation
{
    //имитационное моделирование СМО различных типов

    public class RunQS
    {
        public ErrorMessage em = new ErrorMessage();


        // запустить СМО и получить ее модель
        // M/M/S/Q 
        // M/M/S/inf (если Q == int.MaxValue //Models/CommonModels.cs/QueueBuffer)
        // M/M/inf (если S == int.MaxValue)
        public bool RunQS_GetModelAndProblem(GenerationErrorSettings ges, out SimulationModeller modeller, out Problem problem, double La = 1, double Mu = 2, int S = 1, int Q = 0,
                                        int maxRealTime = 30 * 60, int maxEventsAmount = 1_000_000, double maxModelationTime = 10000)
        {
            problem = InitProblem(ges, La, Mu, S, Q, maxRealTime, maxEventsAmount, maxModelationTime);

            modeller = new();

            try
            {
                modeller.Simulate(problem);
            }
            catch (Exception e)
            {
                em.Add_ErrorMsg($"{e.Message}.");
                return false;
            }

            return true;
        }

        

         // Метод инициализирует задачу (с конечным и бесконечным числом обработчиков).
         // M=La / M=Mu / S / Q    (если (S или Q) == Inf, то == int.MaxValue)
        internal static Problem InitProblem(GenerationErrorSettings ges, double La = 1, double Mu = 2, int S = 1, int Q = 0,
                                                           int maxRealTime = 30 * 60, int maxEventsAmount = 1_000_000, double maxModelationTime = 10000)
        {
            Dictionary<string, List<IModellingAgent>> linkList;
            List<IModellingAgent> agentList;
            Problem problem = new();

            var source = new Source(new ExponentialDistribution(La));

            //если система M/M/inf
            if (S == int.MaxValue)
            {
                var serviceBlock = new InfServiceBlocks(new ExponentialDistribution(Mu));

                linkList = new() {
                    {
                        source.Id,
                        new() {
                            serviceBlock
                        }
                    }
                };

                agentList = new() {
                    source,
                    serviceBlock
                };

                problem.Name = $"Example M={La}/M={Mu}/Inf";
                problem.AddAgentForStatistic(serviceBlock);
            }
            else //если система M/M/S/Q или M/M/S/inf 
            {
                var serviceBlock = new FinServiceBlocks(new ExponentialDistribution(Mu), S);

                linkList = new() {
                    {
                        source.Id,
                        new() {
                            serviceBlock
                        }
                    }
                };

                agentList = new() {
                    source,
                    serviceBlock
                };

                if (Q != 0)
                {
                    var queue = new QueueBuffer(Q);
                    serviceBlock.BindBuffer(queue);
                    agentList.Add(queue);
                }

                if(Q == int.MaxValue)
                    problem.Name = $"Example M={La}/M={Mu}/n={S}/Inf";
                else
                    problem.Name = $"Example M={La}/M={Mu}/n={S}/c={Q}";
                problem.AddAgentForStatistic(serviceBlock);
            }


            problem.Agents = agentList;
            problem.CreateAt = DateTime.Now;
            problem.Links = linkList;
            problem.MaxRealTime = maxRealTime;
            problem.MaxEventsAmount = maxEventsAmount;
            problem.MaxModelationTime = maxModelationTime;
            problem.GenerationErrorSettings = ges;

            return problem;
        }



        /*
         * 
         * 
        // Метод инициализирует задачу с бесконечным числом обработчиков.
        // M/M/inf/inf 
        internal static Problem InitInfServiceBlockProblem(GenerationErrorSettings ges, double La = 0.2, double Mu = 0.5,
                                                           int maxRealTime = 30 * 60, int maxEventsAmount = 1_000_000, double maxModelationTime = 10000)
        {
            Dictionary<string, List<IModellingAgent>> linkList;
            List<IModellingAgent> agentList;

            var source = new Source(new ExponentialDistribution(La));
            var serviceBlock = new InfServiceBlocks(new ExponentialDistribution(Mu));

            linkList = new() {
                {
                    source.Id,
                    new() {
                        serviceBlock
                    }
                }
            };

            agentList = new() {
                source,
                serviceBlock
            };

            Problem problem = new()
            {
                Agents = agentList,
                CreateAt = DateTime.Now,
                ReflectionType = $"Example M={La}/M={Mu}/Inf",
                Links = linkList,
                MaxRealTime = maxRealTime,
                MaxEventsAmount = maxEventsAmount,
                MaxModelationTime = maxModelationTime,
                generationErrorSettings = ges
            };

            problem.AddAgentForStatistic(serviceBlock); 
            return problem;
        }


         // Метод инициализирует задачу с конечным числом обработчиков.
         // M/M/n/c
         // M=La / M=Mu / n=S / c=Q
        internal static Problem InitFinServiceBlockProblem(GenerationErrorSettings ges, double La = 1, double Mu = 2, int S = 1, int Q = 0,
                                                           int maxRealTime = 30 * 60, int maxEventsAmount = 1_000_000, double maxModelationTime = 10000)
        {
            Dictionary<string, List<IModellingAgent>> linkList;
            List<IModellingAgent> agentList;

            var source = new Source(new ExponentialDistribution(La));
            var serviceBlock = new FinServiceBlocks(S, new ExponentialDistribution(Mu));

            linkList = new() {
                {
                    source.Id,
                    new() {
                        serviceBlock
                    }
                }
            };

            agentList = new() {
                source,
                serviceBlock
            };

            if( Q != 0 ) {
                var queue = new QueueBuffer( Q ); 
                serviceBlock.BindBuffer( queue );
                agentList.Add( queue );
            }

            Problem problem = new()
            {
                Agents = agentList,
                CreateAt = DateTime.Now,
                ReflectionType = $"Example M={La}/M={Mu}/n={S}/c={Q}",
                Links = linkList,
                MaxRealTime = maxRealTime,
                MaxEventsAmount = maxEventsAmount,
                MaxModelationTime = maxModelationTime,                
                generationErrorSettings = ges
            };

            problem.AddAgentForStatistic(serviceBlock);
            return problem;
        }


        */


    }
}
