# Ответы на вопросы по DataCollector

---

## 1. Механизм регистрации данных (Связь Агентов и Коллектора)

### Как Агент передает данные?

**Прямая ссылка НЕ у агента.** Паттерн «Наблюдатель» НЕ используется. Вместо этого используется **централизованный опрос (polling)** из главного цикла моделирования.

Класс `SimulationModeller` владеет и планировщиком (`Supervisor`), и коллектором (`DataCollector`). На каждой итерации цикла моделирования именно `SimulationModeller` вызывает метод сбора:

```csharp
// SimQ.Core/Statistic/SimulationModeller.cs
while (!isDone) {
    Event nextEvent = supervisor.GetNextEvent();
    dataCollector.AddState(nextEvent.ModelTimeStamp - lastEventModelationTime, problem.AgentsForStatistic);
    lastEventModelationTime = nextEvent.ModelTimeStamp;
    supervisor.FireEvent(nextEvent);
}
```

То есть агенты **не знают** о коллекторе. Связь однонаправленная: коллектор опрашивает агентов через интерфейс.

### Какой метод вызывается?

```csharp
dataCollector.AddState(double deltaT, List<IModellingAgent> agents)
```

Внутри `AddState` для каждого агента вызывается:
```csharp
int current_state = (agent as IAgentStatistic).GetCurrentState();
```

Интерфейс `IAgentStatistic` имеет единственный метод:
```csharp
public interface IAgentStatistic {
    public int GetCurrentState();
}
```

---

## 2. Структура «сырых» данных

### Что именно хранится внутри?

**Аккумулятор длительностей**, а НЕ полный лог переходов.

Основная структура:
```csharp
public Dictionary<IModellingAgent, Dictionary<int, double>> agentsStatisticData = [];
```

Это: `Агент → (Состояние → Суммарное время пребывания в этом состоянии)`.

При каждом событии к текущему состоянию агента прибавляется `deltaT` — время, прошедшее с предыдущего события:
```csharp
agentsStatisticData[agent][current_state] += deltaT;
```

Это означает, что:
- Полная история переходов **не хранится** (экономия памяти)
- Нормализация (`/ totalTime`) даёт вероятности и происходит позже, в `StatesStatistic` или `BuildResult()`

### Типы данных

Используются **обычные** `Dictionary<,>` — **НЕ потокобезопасные**. Моделирование **однопоточное** (один цикл `while` в `SimulationModeller.Simulate()`), поэтому `ConcurrentDictionary` не нужен.

---

## 3. Жизненный цикл и сброс

### Когда создается объект?

**Новый экземпляр на каждый запуск.** В методе `SimulationModeller.Simulate()`:
```csharp
dataCollector = new DataCollector(problem);
```

Никакого метода `Clear()` нет — объект одноразовый.

### Момент фиксации

Данные передаются **по явному вызову после завершения цикла** моделирования:
```csharp
// После выхода из while (!isDone):
dataCollector.GetAllCalls(problem.Agents);       // подсчёт заявок
var result = dataCollector.BuildResult(EndRealTime); // формирование итогового объекта
```

Метод `BuildResult()` нормализует данные (делит на `totalTime`) и создаёт объект `SimulationResultData`, который далее сохраняется в MongoDB.

---

## 4. Особенности учета времени

### Модельное vs Реальное

Коллектор **не обращается к глобальному планировщику напрямую**. Модельное время передаётся ему извне как разность `deltaT`:

```csharp
deltaT = nextEvent.ModelTimeStamp - lastEventModelationTime
```

`ModelTimeStamp` — это абсолютное модельное время следующего события, которое хранится в объекте `Event`, полученном из `Supervisor`. Разность — время пребывания системы в текущем состоянии между двумя последовательными событиями.

Коллектор также отслеживает:
- `CurrentModelationTime += deltaT` — накопленное модельное время
- `CurrentEventsAmount++` — счётчик событий

Реальное время считается отдельно в `SimulationModeller`:
```csharp
EndRealTime = (DateTime.Now - StartRealTime).TotalSeconds;
```

### Дискретность

В дискретно-событийной модели **два события не могут произойти в один и тот же момент модельного времени** (если `deltaT == 0`, то к состоянию прибавляется 0 — фактически ничего не меняется). Это корректно, т.к. мы измеряем не «сколько раз были в состоянии», а «сколько времени провели в состоянии».

---

## 5. Интеграция с Ядром (SimQ.Core)

### Где он живет?

`DataCollector` находится в пространстве имён:
```
SimQ.Core.Statistic
```

Это тот же проект `SimQ.Core`, но отдельная папка `Statistic/`. Рядом лежат:
- `StatisticCollector.cs`
- `StatesStatistic.cs`
- `SimulationModeller.cs`

### Зависимости

`DataCollector` работает с **абстрактным интерфейсом**:
- `IModellingAgent` — для идентификации агента (поле `Id`, `Type`)
- `IAgentStatistic` — для получения текущего состояния (`GetCurrentState()`)

Он **не зависит** от конкретных классов `Source`, `QueueBuffer`, `ServiceBlock`. Любой новый тип агента, реализующий `IAgentStatistic`, автоматически будет учитываться коллектором без изменения его кода.

Список агентов для сбора статистики задаётся в объекте `Problem`:
```csharp
problem.AgentsForStatistic  // List<IModellingAgent>
```

---

## 6. Критерий остановки (бонус)

`DataCollector` также отвечает за определение завершения моделирования через свойство `isDone`:
```csharp
public bool isDone =>
    CurrentModelationTime >= problem.MaxModelationTime
    || CurrentEventsAmount >= problem.MaxEventsAmount
    || CurrentGenerationError <= problem.GenerationErrorSettings.MinGenerationError;
```

Три условия остановки (ИЛИ):
1. Достигнуто максимальное модельное время
2. Достигнуто максимальное число событий
3. Погрешность генерации (расстояние Колмогорова между последовательными эмпирическими распределениями) стала меньше заданного порога `MinGenerationError`

Перерасчёт расстояния Колмогорова выполняется **внутри** `AddState()` с нарастающим шагом:
```
шаг_проверки = GenerationErrorCheckStep * GenerationErrorCheckStepModifier * номер_проверки
```

---

## Итоговая схема

```
SimulationModeller.Simulate()
  │
  ├── new DataCollector(problem)        ← создание коллектора
  │
  ├── while (!isDone):
  │     ├── supervisor.GetNextEvent()   ← получение следующего события
  │     ├── dataCollector.AddState(deltaT, agents)
  │     │     ├── CurrentModelationTime += deltaT
  │     │     ├── CurrentEventsAmount++
  │     │     ├── foreach agent:
  │     │     │     └── agentsStatisticData[agent][state] += deltaT
  │     │     └── if (нужна проверка):
  │     │           └── KD.KolmogorovDistance() → CurrentGenerationError
  │     └── supervisor.FireEvent(nextEvent)     ← выполнение события
  │
  ├── dataCollector.GetAllCalls(agents)          ← подсчёт заявок
  └── dataCollector.BuildResult(endRealTime)     ← формирование SimulationResultData
        ├── нормализация: rawStates[i] / totalTime → вероятности
        ├── расчёт средних: sum(i * P(i))
        └── return SimulationResultData { AgentResults, ... }
```
