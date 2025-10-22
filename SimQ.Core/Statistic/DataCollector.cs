using SimQ.Core.Models;
using SimQ.Core.Models.Base;
using SimQ.Domain.Models.ProblemAggregation;
using SimQCore.Library.CompareDists;
using SimQCore.Modeller;
using Problem = SimQ.Core.Models.Problem;

namespace SimQ.Core.Statistic {
    public class DataCollector {
        /// <summary>
        /// Промежуточные результаты эмпирического распределения, используемые для просчёта расстояния Колмогорова.
        /// TODO: Изменить?
        /// </summary>
        private Dictionary<IModellingAgent,Dictionary<int,double>> prevNormalizedStats;

        /// <summary>
        /// Номер события, на момент которого необходимо выполнить перерасчёт расстояния Колмогорова.
        /// </summary>
        private int GenerationErrorCheckEventsAmount;

        /// <summary>
        /// Количество раз выполнения перерасчёта расстояния Колмогорова.
        /// </summary>
        private int GenerationErrorChecksAmount = 0;

        /// <summary>
        /// Моделируемая задача.
        /// </summary>
        private Problem problem;

        /// <summary>
        /// Текущее количество событий.
        /// </summary>
        public double CurrentEventsAmount = 0;

        /// <summary>
        /// Текущий показатель ошибки генерации.
        /// </summary>
        public double CurrentGenerationError = 1;

        /// <summary>
        /// Текущее модельное время.
        /// </summary>
        public double CurrentModelationTime = 0;

        /// <summary>
        /// Флаг определяет, закончено ли моделирование текущей задачи по статистическим измерениям.
        /// </summary>
        public bool isDone =>
            CurrentModelationTime >= problem.MaxModelationTime
                || CurrentEventsAmount >= problem.MaxEventsAmount
                || CurrentGenerationError <= problem.GenerationErrorSettings.MinGenerationError;

        public string _id = Guid.NewGuid().ToString("N");
        public DateTime Date = DateTime.Now;
        public string Name;

        public int totalCalls = 0;
        //public int totalStates = 0;
        public Dictionary<IModellingAgent,Dictionary<int,double>> agentsStatisticData = [];

        public DataCollector(Problem problem)
        {
            this.problem = problem;
            GenerationErrorCheckEventsAmount = problem.GenerationErrorSettings.GenerationErrorCheckStep;

            SetupStates(problem.AgentsForStatistic);
        }

        public void SetupStates(List<IModellingAgent> agents) { 
            if (agents != null)
                foreach (IModellingAgent agent in agents)
                    /*if( agent is IAgentStatistic )*/
                    agentsStatisticData.Add(agent, []);
        }
        
        public void AddState( double deltaT, List<IModellingAgent> agents ) {
            CurrentModelationTime += deltaT;
            CurrentEventsAmount++;
            //totalStates++;

            foreach( IModellingAgent agent in agents ) {
                if( agentsStatisticData.ContainsKey(agent) ) {
                    int current_state = (agent as IAgentStatistic).GetCurrentState();
                    if (agentsStatisticData[agent].ContainsKey(current_state)) {
                        agentsStatisticData[agent][current_state] += deltaT;
                    } else {
                        agentsStatisticData[agent].Add(current_state, deltaT);
                    }
                }
            }

            // Перерасчёт расстояния Колмогорова
            if( CurrentEventsAmount >= GenerationErrorCheckEventsAmount ) {
                // Временно реализация такова, но впредь следует переделать
                Dictionary<IModellingAgent, Dictionary<int, double>> currentNormalizedStats = agentsStatisticData.ToDictionary(
                    k => k.Key,
                    v => v.Value.ToDictionary(
                        k => k.Key,
                        v => v.Value / CurrentModelationTime
                    )
                );

                if( prevNormalizedStats != null ) {
                    foreach( (IModellingAgent agent, Dictionary<int, double> states) in currentNormalizedStats ) {
                        // Предыдущий замер (prevNormalizedStats) может иметь
                        // меньшее количество состояний, чем текущее (states)
                        KD.KolmogorovDistance(
                            [..states.Values], [..prevNormalizedStats[agent].Values],
                            out double genError
                        );
                        CurrentGenerationError = Math.Min(genError, CurrentGenerationError);
                    }
                }

                prevNormalizedStats = currentNormalizedStats;

                GenerationErrorChecksAmount++;
                GenerationErrorCheckEventsAmount +=
                    problem.GenerationErrorSettings.GenerationErrorCheckStep
                    * problem.GenerationErrorSettings.GenerationErrorCheckStepModifier
                    * GenerationErrorChecksAmount;
                Console.WriteLine( CurrentEventsAmount );
            }
        }

        public void GetAllCalls( List<IModellingAgent> agents ) {
            foreach( IModellingAgent agent in agents ) {
                if( agent.Type == AgentType.SOURCE ) {
                    totalCalls += ( ( BaseSource )agent ).CallsCreated;
                }
            }
        }

        //public static string LoadStatesToJson(string id)
        //{
        //    if( Storage.collection.CollectionNamespace.CollectionName != "States" ) {
        //        Storage.SetCurrentCollection( "States" );
        //    }
        //    return Storage.GetDocument( id );
        //}

        //public static StatisticCollector LoadStatesToObject( string id ) {
        //    if( Storage.collection.CollectionNamespace.CollectionName != "States" )
        //        Storage.SetCurrentCollection( "States" );
        //    var doc = Storage.GetDocument(id);
        //    return JsonConvert.DeserializeObject<StatisticCollector>(doc);
        //}

        //public void SaveStates()
        //{
        //    if (Storage.collection.CollectionNamespace.CollectionName != "States") Storage.SetCurrentCollection("States");
        //    var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        //    Storage.CreateDocument(json);
        //}
    }
}