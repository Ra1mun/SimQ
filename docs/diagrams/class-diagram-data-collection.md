# Диаграмма классов: Подсистема сбора данных

## Mermaid-диаграмма

```mermaid
classDiagram
    direction TB

    %% ═══════ Интерфейсы ═══════

    class IModellingAgent {
        <<interface>>
        +string Id
        +double NextEventTime
        +string EventTag
        +AgentType Type
        +BaseCall DoEvent(double T)
        +bool IsActive()
    }

    class IAgentStatistic {
        <<interface>>
        +int GetCurrentState()
    }

    %% ═══════ Агенты (реализации) ═══════

    class BaseSource {
        <<abstract>>
        +AgentType Type = SOURCE
        +int CallsCreated
        +BaseCall DoEvent(double T)
    }

    class BaseServiceBlock {
        <<abstract>>
        +AgentType Type = SERVICE_BLOCK
        +BaseCall ProcessCall
        +bool IsFree()
        +bool TakeCall(BaseCall, double)
        +List~BaseBuffer~ BindedBuffers
    }

    class BaseBuffer {
        <<abstract>>
        +AgentType Type = BUFFER
        +bool IsFull
        +bool IsEmpty
    }

    %% ═══════ Ядро моделирования ═══════

    class Problem {
        +string Name
        +int MaxRealTime
        +int MaxEventsAmount
        +double MaxModelationTime
        +GenerationErrorSettings GenerationErrorSettings
        +List~IModellingAgent~ Agents
        +List~IModellingAgent~ AgentsForStatistic
        +Dictionary~string, List~ Links
        +AddAgentForStatistic(IModellingAgent)
    }

    class Event {
        <<struct>>
        +double ModelTimeStamp
        +IModellingAgent Agent
    }

    class Supervisor {
        -List~IModellingAgent~ activeModels
        +List~IModellingAgent~ AllAgents
        +Dictionary~string, List~ Links
        +Event GetNextEvent()
        +void FireEvent(Event)
    }

    %% ═══════ Подсистема сбора данных ═══════

    class SimulationModeller {
        -Problem problem
        -DateTime StartRealTime
        +double EndRealTime
        +DataCollector dataCollector
        +void Simulate(Problem)
    }

    class DataCollector {
        -Problem problem
        -Dictionary~IModellingAgent, Dictionary~ prevNormalizedStats
        -int GenerationErrorCheckEventsAmount
        -int GenerationErrorChecksAmount
        +double CurrentEventsAmount
        +double CurrentGenerationError
        +double CurrentModelationTime
        +bool isDone
        +int totalCalls
        +Dictionary~IModellingAgent, Dictionary~int,double~~ agentsStatisticData
        +DataCollector(Problem)
        +void SetupStates(List~IModellingAgent~)
        +void AddState(double deltaT, List~IModellingAgent~)
        +void GetAllCalls(List~IModellingAgent~)
        +SimulationResultData BuildResult(double endRealTime)
    }

    class StatisticCollector {
        +Dictionary~IModellingAgent, double~ average
        +Dictionary~IModellingAgent, Dictionary~int,double~~ states
        -StatesStatistic StatesStat
        +void CollectStatistic(DataCollector)
        -void CalculateAverages(...)
    }

    class StatesStatistic {
        +Dictionary~IModellingAgent, Dictionary~int,double~~ states
        +StatesStatistic(DataCollector)
        -void NormalizeStatesProbs(...)
        +bool Get_EmpDist(out double[] Y)
        +string EmpDistToString()
    }

    class KD {
        <<static>>
        +void KolmogorovDistance(double[], double[], out double)
    }

    %% ═══════ Результаты ═══════

    class SimulationResultData {
        +double EndRealTime
        +double MaxRealTime
        +double CurrentEventsAmount
        +double MaxEventsAmount
        +double CurrentModelationTime
        +double MaxModelationTime
        +double CurrentGenerationError
        +double MinGenerationError
        +int TotalCalls
        +List~AgentStatisticResult~ AgentResults
        +string ToText(string problemName)
    }

    class AgentStatisticResult {
        +string AgentId
        +AgentType AgentType
        +double Average
        +Dictionary~string, double~ StatesProbabilities
    }

    class GenerationErrorSettings {
        <<struct>>
        +int GenerationErrorCheckStep = 10000
        +int GenerationErrorCheckStepModifier = 3
        +double MinGenerationError = 0.00001
    }

    %% ═══════ Связи ═══════

    IModellingAgent <|.. BaseSource : implements
    IModellingAgent <|.. BaseServiceBlock : implements
    IModellingAgent <|.. BaseBuffer : implements
    IAgentStatistic <|.. BaseSource : implements
    IAgentStatistic <|.. BaseServiceBlock : implements
    IAgentStatistic <|.. BaseBuffer : implements

    SimulationModeller --> Supervisor : создаёт и использует
    SimulationModeller --> DataCollector : создаёт и вызывает AddState()
    SimulationModeller --> Problem : получает на вход

    Supervisor --> Event : возвращает из GetNextEvent()
    Supervisor --> IModellingAgent : управляет коллекцией

    DataCollector --> Problem : хранит ссылку
    DataCollector --> IModellingAgent : опрашивает через IAgentStatistic
    DataCollector --> KD : вызывает KolmogorovDistance()
    DataCollector --> SimulationResultData : создаёт в BuildResult()

    StatisticCollector --> DataCollector : принимает в CollectStatistic()
    StatisticCollector --> StatesStatistic : создаёт

    StatesStatistic --> DataCollector : читает agentsStatisticData

    SimulationResultData --> AgentStatisticResult : содержит список
    Problem --> GenerationErrorSettings : содержит
```

---

## Текстовое описание связей

```
SimulationModeller
  ├── владеет ──► DataCollector        (создаёт в Simulate(), вызывает AddState на каждом событии)
  ├── владеет ──► Supervisor           (планировщик, выдаёт события)
  └── получает ─► Problem              (конфигурация задачи)

DataCollector
  ├── хранит ссылку ─► Problem         (для параметров остановки и GenerationErrorSettings)
  ├── опрашивает ────► IAgentStatistic  (GetCurrentState() у каждого агента)
  ├── использует ────► KD              (расстояние Колмогорова для оценки погрешности)
  └── создаёт ──────► SimulationResultData  (итоговый результат в BuildResult())

StatisticCollector (альтернативный путь обработки)
  ├── принимает ─► DataCollector        (читает agentsStatisticData)
  └── создаёт ──► StatesStatistic       (нормализация, эмпирическое распределение)

SimulationResultData
  └── содержит ─► List<AgentStatisticResult>  (статистика по каждому агенту)
```

---

## Роли классов

| Класс | Роль в подсистеме |
|-------|-------------------|
| `SimulationModeller` | **Оркестратор** — запускает цикл, связывает планировщик и коллектор |
| `Supervisor` | **Планировщик событий** — выбирает ближайшее событие, исполняет его |
| `DataCollector` | **Коллектор** — накапливает сырые данные (время в состояниях), проверяет сходимость, формирует итог |
| `StatisticCollector` | **Процессор** — альтернативная обработка (средние, ковариации) |
| `StatesStatistic` | **Нормализатор** — делит время на общее, даёт вероятности |
| `KD` | **Утилита** — вычисление расстояния Колмогорова |
| `IAgentStatistic` | **Контракт** — единственный метод `GetCurrentState()`, позволяет коллектору не зависеть от конкретных агентов |
| `SimulationResultData` | **DTO результата** — структурированный итог для сохранения в БД |
