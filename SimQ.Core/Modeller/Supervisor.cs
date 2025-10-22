using SimQ.Core.Models;
using SimQ.Core.Models.Base;

namespace SimQCore.Modeller {
    /// <summary>
    /// Класс представляет Диспетчера СМО.
    /// </summary>
    /// <remarks>
    /// Диспетчер обеспечивает связь между объектами СМО.
    /// Передача заявок от агента к агенту осуществляется внутри данного класса.
    /// </remarks>
    public class Supervisor {
        /// <summary>
        /// Коллекция, содержащая всех активных агентов, используемых в моделировании.
        /// </summary>
        private List<IModellingAgent> activeModels;

        /// <summary>
        /// Коллекция методов, вызываемых по наступлению событий.
        /// </summary>
        public static Dictionary<string, Func<IModellingAgent, List<IModellingAgent>, double, bool>> Actions = [];

        /// <summary>
        /// Коллекция связей агентов. Используется при вызове того или иного события.
        /// Позволяет направлять заявки далее выбранному агенту.
        /// </summary>
        public Dictionary<string, List<IModellingAgent>> Links {
            get; set;
        }

        /// <summary>
        /// Коллекция действующих объектов СМО.
        /// Объекты имеют тип <paramref name="AgentModel" />.
        /// </summary>
        public List<IModellingAgent> AllAgents;

        /// <summary>
        /// Метод позволяет добавить в коллекцию новое действие, 
        /// совершаемое при наступлении события агента с тэгом EventTag.
        /// </summary>
        /// <param name="EventTag">Тэг агента, использующийся для определения действия, 
        /// выполняемого при наступлении события.
        /// </param>
        /// <param name="Action">Действие, выполняемое при наступлении события.</param>
        public static void AddAction( string EventTag,
            Func<IModellingAgent, List<IModellingAgent>, double, bool> Action )
        {
            if( !Actions.ContainsKey( EventTag ) ) {
                Actions.Add( EventTag, Action );
            }
        }

        public Supervisor( Problem problem ) => Setup( problem );

        /// <summary>
        /// Метод подготавливает диспетчера к моделированию задачи.
        /// </summary>
        public void Setup( Problem problem ) {
            AllAgents = problem.Agents;
            Links = problem.Links;

            activeModels = new();
            foreach( IModellingAgent agent in AllAgents ) {
                if( agent.IsActive() ) {
                    activeModels.Add( agent );
                }
            }

            // Установление ещё каких-либо настроек диспетчера (в зависимости от задачи)
        }


        /// <summary>
        /// Метод выполняет действие, совершаемое при возникшем событии.
        /// </summary>
        /// <param name="e">Описание происходящего события.</param>
        public void FireEvent( Event e ) {
            IModellingAgent Agent = e.Agent;
            List<IModellingAgent> AgentLinks = Links.ContainsKey( Agent.Id )
                                            ? Links[ Agent.Id ]
                                            : null;
            Actions [Agent.EventTag]( Agent, AgentLinks, e.ModelTimeStamp );
        }

        /// <summary>
        /// Метод возвращает следующее моделируемое событие.
        /// </summary>
        /// <returns>Следующее событие <paramref name="Event"/></returns>
        public Event GetNextEvent() {
            IModellingAgent nextAgent = null;
            double minT = double.PositiveInfinity;

            foreach( IModellingAgent agent in activeModels ) {
                double agentEventTime = agent.NextEventTime;
                if( agentEventTime <= minT ) {
                    minT = agentEventTime;
                    nextAgent = agent;
                }
            }

            if( nextAgent == null ) {
                throw new NotSupportedException();
            }

            Event newEvent = new() {
                ModelTimeStamp = minT,
                Agent = nextAgent,
            };
            return newEvent;
        }
    }
}
