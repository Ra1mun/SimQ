using SimQ.Core.Models.Base;

namespace SimQ.Core.Models {
    public interface IAgentStatistic {
        /** Метод возвращает текущее состояние агента моделирования. */
        public int GetCurrentState();
    }
    
    /** Интерфейс, реализуемый моделями агентов, позволяющий выводить их собственный набор результатов. */
    interface IResultableModel {
        /** Метод выводит результаты агента. */
        public string GetResult();
    }
    
    public struct GenerationErrorSettings {
        /// <summary>
        /// Шаг, для определения момента перерасчёта расстояния Колмогорова (n).
        /// </summary>
        public int GenerationErrorCheckStep = 10_000;

        /// <summary>
        /// Множитель, используемый для определения следующего момента перерасчёта расстояния Колмогорова (k).
        /// </summary>
        public int GenerationErrorCheckStepModifier = 3;

        /// <summary>
        /// Погрешность генерации, при достижении которой моделирование будет окончено (eps).
        /// </summary>
        public double MinGenerationError = 0.00001;

        public GenerationErrorSettings() {}
    }
    
    public struct Event {
        /// <summary>
        /// Модельное время возникшего события.
        /// </summary>
        public double ModelTimeStamp;
        /// <summary>
        /// Агент, вызвавший событие.
        /// </summary>
        public IModellingAgent Agent;
    }
    public class Problem {
        /// <summary>
        /// Дата создания задачи.
        /// </summary>
        public DateTime CreateAt;
        /// <summary>
        /// Наименование задачи.
        /// </summary>
        public string Name;
        /// <summary>
        /// Реальное время, в течение которого будет выполняться моделирование (в секундах).
        /// </summary>
        public int MaxRealTime = 30 * 60;
        /// <summary>
        /// Максимальное количество событий, при достижении которого моделирование будет окончено.
        /// </summary>
        public int MaxEventsAmount = 1_000_000;
        /// <summary>
        /// Максимальное модельное время, при достижении которого моделирование будет окончено.
        /// </summary>
        public double MaxModelationTime = 1_000;
        /// <summary>
        /// Настройки вычисления ошибки генерации.
        /// </summary>
        public GenerationErrorSettings GenerationErrorSettings = new();
        /// <summary>
        /// Список агентов, участвующих в системе.
        /// </summary>
        public List<IModellingAgent> Agents;
        /// <summary>
        /// Список связей для всех существующих агентов.
        /// </summary>
        public Dictionary<string, List<IModellingAgent>> Links;
        public readonly List<IModellingAgent> AgentsForStatistic = [];

        public void AddAgentForStatistic( IModellingAgent agent )  
            => AgentsForStatistic.Add( agent );
        
    }
}
