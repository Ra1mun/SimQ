# Сбор и обработка данных имитационного моделирования

## Общая схема по шагам

```
TasksService.RunTaskWithCallback()
  │
  ├─► ProblemConvertor.Convert(dalProblem) → Problem (модель задачи ядра)
  │
  ├─► SimulationModeller.Simulate(problem)
  │     │
  │     ├─► new Supervisor(problem)              — планировщик событий
  │     ├─► new DataCollector(problem)           — сборщик статистики
  │     │     └─► SetupStates(problem.AgentsForStatistic)
  │     │           └─► agentsStatisticData[agent] = {} для каждого агента
  │     │
  │     └─► while (!isDone):
  │           ├─► supervisor.GetNextEvent() → Event { ModelTimeStamp, Agent }
  │           ├─► dataCollector.AddState(deltaT, agents)       ← СБОР
  │           │     ├── CurrentModelationTime += deltaT
  │           │     ├── CurrentEventsAmount++
  │           │     ├── foreach agent in agents:
  │           │     │     state = (agent as IAgentStatistic).GetCurrentState()
  │           │     │     agentsStatisticData[agent][state] += deltaT
  │           │     └── if (пора проверять погрешность):
  │           │           нормализация текущих данных / CurrentModelationTime
  │           │           KD.KolmogorovDistance(текущее, предыдущее) → CurrentGenerationError
  │           └─► supervisor.FireEvent(nextEvent) — исполнение события, смена состояний
  │
  ├─► dataCollector.GetAllCalls(agents)          — подсчёт созданных заявок у Source
  │
  ├─► dataCollector.BuildResult(endRealTime)     ← ОБРАБОТКА
  │     ├── foreach (agent, rawStates) in agentsStatisticData:
  │     │     probabilities[state] = rawStates[state] / totalTime    ← нормализация
  │     │     average = Σ(state * P(state))                          ← среднее
  │     │     → AgentStatisticResult { AgentId, AgentType, Average, StatesProbabilities }
  │     └── return SimulationResultData {
  │              EndRealTime, MaxRealTime,
  │              CurrentEventsAmount, MaxEventsAmount,
  │              CurrentModelationTime, MaxModelationTime,
  │              CurrentGenerationError, MinGenerationError,
  │              TotalCalls, AgentResults[]
  │          }
  │
  ├─► resultData.ToText(problemName)             — текстовое представление
  │
  ├─► new DalResult { ProblemId, TaskId, Text, Data = resultData }
  └─► _resultRepository.AddAsync(newResult)      — сохранение в MongoDB
```

---

## Фаза 1: Сбор данных (`DataCollector.AddState`)

**Что происходит:** на каждом событии фиксируется, сколько модельного времени система провела в текущей конфигурации состояний.

**Входные данные:**
- `deltaT` — время между предыдущим и текущим событием (разность `ModelTimeStamp`)
- `agents` — список агентов, по которым ведётся статистика (`problem.AgentsForStatistic`)

**Алгоритм:**
```
1. CurrentModelationTime += deltaT
2. CurrentEventsAmount++
3. Для каждого агента:
     state = agent.GetCurrentState()   // целое число: 0, 1, 2, ...
     agentsStatisticData[agent][state] += deltaT
4. Если CurrentEventsAmount >= порог проверки:
     Нормализовать текущие данные (/ CurrentModelationTime)
     Сравнить с предыдущим замером через расстояние Колмогорова
     Обновить CurrentGenerationError
     Увеличить порог проверки (шаг * множитель * номер_проверки)
```

**Формат хранения (в памяти):**
```
agentsStatisticData = {
    Agent_Source_1:       { 0: 45.2,  1: 112.8,  2: 3.7 },
    Agent_ServiceBlock_1: { 0: 98.3,  1: 63.4 },
    Agent_Buffer_1:       { 0: 20.1,  1: 55.0,  2: 40.3,  3: 46.3 }
}
```
Ключ — номер состояния (кол-во заявок в узле), значение — суммарное время пребывания.

---

## Фаза 2: Обработка данных (`DataCollector.BuildResult`)

**Что происходит:** сырые аккумулированные времена преобразуются в вероятности и средние значения.

**Алгоритм:**
```
Для каждого агента:
  1. Нормализация:
       P(state) = time_in_state / totalModelTime
       (получаем эмпирическое распределение вероятностей)
  
  2. Среднее значение:
       Average = Σ (state × P(state))
       (мат. ожидание числа заявок в узле)

  3. Формирование результата:
       AgentStatisticResult {
           AgentId = "a4",
           AgentType = SERVICE_BLOCK,
           Average = 1.37,
           StatesProbabilities = { "0": 0.412, "1": 0.334, "2": 0.254 }
       }
```

**Пример выхода:**
```json
{
  "endRealTime": 4.2,
  "maxRealTime": 1800,
  "currentEventsAmount": 50000,
  "maxEventsAmount": 1000000,
  "currentModelationTime": 161.7,
  "maxModelationTime": 1000,
  "currentGenerationError": 0.00032,
  "minGenerationError": 0.00001,
  "totalCalls": 12847,
  "agentResults": [
    { "agentId": "a1", "agentType": "SOURCE",        "average": 0.91, "statesProbabilities": {"0": 0.09, "1": 0.91} },
    { "agentId": "a3", "agentType": "BUFFER",        "average": 6.34, "statesProbabilities": {"0": 0.01, "1": 0.03, ...} },
    { "agentId": "a4", "agentType": "SERVICE_BLOCK", "average": 0.87, "statesProbabilities": {"0": 0.13, "1": 0.87} }
  ]
}
```

---

## Фаза 3: Сохранение результата

```
TasksService.RunTaskWithCallback():
  1. resultData = dataCollector.BuildResult(endRealTime)
  2. text = resultData.ToText(problemName)          — человекочитаемое описание
  3. simulationTask.ResultData = resultData         — обновление задачи в БД
  4. new DalResult {
       ProblemId, ProblemName, TaskId,
       Text = text,
       Data = resultData
     }
  5. _resultRepository.AddAsync(newResult)          — INSERT в MongoDB (коллекция "results")
```

---

## Фаза 4: Выдача клиенту

```
HTTP GET /Results/v1/results?problemId=xxx
  → ResultsController.GetResults()
    → ResultService.GetAllResultsAsync()
      → ResultRepository.GetManyAsync(predicate)
        → MongoDB.Find(filter)          → List<Result>
      → AutoMapper: Result → ResultDto  (с вложенными SimulationResultDataDto, AgentStatisticResultDto)
    → return ResultListResponse { Results[] }
  → JSON response → клиент
```

---

## Критерии остановки моделирования

| Условие | Параметр по умолчанию | Смысл |
|---------|----------------------|-------|
| `CurrentModelationTime >= MaxModelationTime` | 1 000 ед. | Достигнуто максимальное модельное время |
| `CurrentEventsAmount >= MaxEventsAmount` | 1 000 000 | Достигнуто максимальное число событий |
| `CurrentGenerationError <= MinGenerationError` | 0.00001 | Распределение стабилизировалось (расстояние Колмогорова мало) |
| `RealTime >= MaxRealTime` | 30 мин | Истекло реальное время выполнения (проверяется в `SimulationModeller.isDone`) |

Остановка происходит при выполнении **любого** из условий (логическое ИЛИ).

---

## Ключевые классы

| Класс | Файл | Роль |
|-------|------|------|
| `SimulationModeller` | `SimQ.Core/Statistic/SimulationModeller.cs` | Главный цикл моделирования |
| `DataCollector` | `SimQ.Core/Statistic/DataCollector.cs` | Сбор сырых данных + построение результата |
| `Supervisor` | `SimQ.Core/Modeller/` | Планировщик событий (календарь) |
| `IAgentStatistic` | `SimQ.Core/Models/Problem.cs` | Интерфейс опроса состояния агента |
| `TasksService` | `SimQ.Core/Services/TasksService.cs` | Управление задачами, вызов моделирования, сохранение |
| `ResultService` | `SimQ.Core/Services/ResultService.cs` | Выдача результатов клиенту через API |
| `ResultRepository` | `SimQ.DAL/Repository/ResultRepository.cs` | Доступ к MongoDB |
