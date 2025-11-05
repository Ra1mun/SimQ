# Архитектура SimQ

Документ описывает архитектурные решения, принципы проектирования и технические детали системы SimQ.

## Оглавление

- [Общий обзор](#общий-обзор)
- [Архитектурные принципы](#архитектурные-принципы)
- [Слои приложения](#слои-приложения)
- [Модуль моделирования](#модуль-моделирования)
- [Паттерны проектирования](#паттерны-проектирования)
- [Работа с данными](#работа-с-данными)
- [Масштабируемость](#масштабируемость)

## Общий обзор

SimQ — это система для моделирования систем массового обслуживания (СМО), построенная на основе многослойной архитектуры с разделением ответственности.

### Основные компоненты

```
┌─────────────────────────────────┐
│   Presentation Layer            │
│   - React Frontend              │
│   - REST API (Controllers)      │
└────────────┬────────────────────┘
             │
┌────────────┴────────────────────┐
│   Business Logic Layer          │
│   - Services                    │
│   - Modelling Engine            │
│   - Statistics Collector        │
└────────────┬────────────────────┘
             │
┌────────────┴────────────────────┐
│   Data Access Layer             │
│   - Repositories                │
│   - MongoDB Driver              │
└────────────┬────────────────────┘
             │
┌────────────┴────────────────────┐
│   Domain Layer                  │
│   - Entities                    │
│   - Value Objects               │
└─────────────────────────────────┘
```

## Архитектурные принципы

### 1. Clean Architecture

Проект следует принципам чистой архитектуры:

- **Независимость от фреймворков**: бизнес-логика не зависит от ASP.NET Core
- **Тестируемость**: каждый слой может быть протестирован независимо
- **Независимость от UI**: логика не зависит от способа представления
- **Независимость от БД**: можно заменить MongoDB на другую БД
- **Независимость от внешних агентов**: бизнес-правила ничего не знают о внешнем мире

### 2. Separation of Concerns

Каждый проект имеет четко определенную ответственность:

- **SimQ.Domain**: Модели данных, не имеет зависимостей
- **SimQ.DAL**: Доступ к данным, зависит только от Domain
- **SimQ.Core**: Бизнес-логика, зависит от Domain и DAL
- **SimQ.WebApi**: Представление, зависит от всех слоев

### 3. Dependency Inversion

Зависимости направлены от внешних слоев к внутренним:

```
WebApi → Core → DAL → Domain
```

Использование интерфейсов для инверсии зависимостей:

```csharp
// Core определяет интерфейс
public interface IProblemRepository { }

// DAL реализует интерфейс
public class ProblemRepository : IProblemRepository { }

// WebApi регистрирует зависимости
services.AddScoped<IProblemRepository, ProblemRepository>();
```

## Слои приложения

### Domain Layer (SimQ.Domain)

**Ответственность**: Определение доменных моделей для хранения в БД.

**Зависимости**: Нет

**Ключевые концепции**:
- Entity классы с MongoDB атрибутами
- Не содержит бизнес-логики
- Только структуры данных

```csharp
public class ProblemEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<AgentEntity> Agents { get; set; }
}
```

### Data Access Layer (SimQ.DAL)

**Ответственность**: Абстракция доступа к базе данных.

**Зависимости**: SimQ.Domain, MongoDB.Driver

**Паттерны**:
- Repository Pattern
- Unit of Work (для транзакций)

```csharp
public interface IProblemRepository
{
    Task<ProblemEntity?> GetByIdAsync(string id, CancellationToken ct);
    Task<List<ProblemEntity>> GetAllAsync(CancellationToken ct);
    Task<ProblemEntity> InsertAsync(ProblemEntity entity, CancellationToken ct);
    Task<bool> UpdateAsync(ProblemEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(string id, CancellationToken ct);
}
```

### Business Logic Layer (SimQ.Core)

**Ответственность**: Вся бизнес-логика приложения.

**Зависимости**: SimQ.Domain, SimQ.DAL

**Ключевые компоненты**:

#### 1. Services
Высокоуровневая бизнес-логика:

```csharp
public interface IProblemService
{
    Task<ProblemResponse?> GetProblemAsync(string id, CancellationToken ct);
    Task<RegisterProblemResponse?> RegisterProblem(RegisterProblemRequest request, CancellationToken ct);
}
```

#### 2. Models
Внутренние модели для моделирования СМО:

```csharp
public interface IModellingAgent
{
    string Id { get; }
    string EventTag { get; }
    Event? NextEvent(double currentTime);
    void ProcessEvent(double currentTime);
    int GetCurrentState();
}
```

#### 3. Modeller
Движок моделирования:

- **Supervisor**: Диспетчер, координирующий работу агентов
- **Agents**: Различные типы агентов (Source, Service, Queue, Sink)

#### 4. Statistic
Сбор и анализ статистики:

- **DataCollector**: Сбор данных в процессе моделирования
- **StatisticCollector**: Вычисление метрик

#### 5. Factories
Создание объектов по типу:

```csharp
public abstract class BaseFactory<T>
{
    public T Create(string type, params object[] args);
    protected abstract T CreateInstance(string type, params object[] args);
}
```

#### 6. Convertors
Преобразование между форматами:

```csharp
public interface IProblemConvertor
{
    Problem Convert(RegisterProblemRequest request);
    ProblemResponse Convert(ProblemEntity entity);
}
```

### Presentation Layer (SimQ.WebApi)

**Ответственность**: REST API и взаимодействие с клиентами.

**Зависимости**: Все предыдущие слои

**Компоненты**:

#### Controllers
```csharp
[ApiController]
[Route("[controller]/v1")]
public class ProblemsController : ControllerBase
{
    private readonly IProblemService _service;
    
    [HttpGet("problems")]
    public async Task<ActionResult<ProblemListResponse>> GetAllProblems(
        CancellationToken ct)
    {
        var response = await _service.GetAllProblemsAsync(ct);
        return response.Total == 0 ? NoContent() : Ok(response);
    }
}
```

#### DTOs
Объекты передачи данных:

```csharp
public record RegisterProblemRequest
{
    public string Name { get; init; }
    public string? Description { get; init; }
    public List<AgentDtoBase> Agents { get; init; }
    public GenerationErrorSettings? GenerationErrorSettings { get; init; }
}
```

## Модуль моделирования

### Архитектура модуля

```
┌──────────────────────────────────────┐
│         Supervisor                   │
│   (Диспетчер событий)                │
└────────┬─────────────────────────────┘
         │
         ├─── Event Queue (приоритетная очередь)
         │
         ├─── Active Agents
         │    ├─── SourceAgent
         │    ├─── ServiceAgent
         │    ├─── QueueAgent
         │    └─── SinkAgent
         │
         └─── Links (связи между агентами)
```

### Алгоритм моделирования

1. **Инициализация**:
   - Создание всех агентов
   - Установка связей между агентами
   - Генерация начальных событий

2. **Основной цикл**:
   ```csharp
   while (modelTime < maxTime && eventsCount < maxEvents)
   {
       // 1. Выбор ближайшего события
       var nextEvent = GetNextEvent();
       
       // 2. Продвижение модельного времени
       modelTime = nextEvent.TimeStamp;
       
       // 3. Обработка события
       var agent = nextEvent.Agent;
       agent.ProcessEvent(modelTime);
       
       // 4. Генерация новых событий
       var newEvent = agent.NextEvent(modelTime);
       if (newEvent != null)
           AddEvent(newEvent);
       
       // 5. Передача заявок связанным агентам
       var linkedAgents = supervisor.Links[agent.EventTag];
       foreach (var linkedAgent in linkedAgents)
       {
           linkedAgent.ReceiveRequest(modelTime);
       }
       
       // 6. Сбор статистики
       statisticCollector.Collect(agent, modelTime);
   }
   ```

3. **Завершение**:
   - Сбор финальной статистики
   - Вычисление метрик
   - Проверка погрешности генерации

### Типы агентов

#### SourceAgent (Источник)
Генерирует заявки с заданной интенсивностью:

```csharp
public class SourceAgent : IModellingAgent
{
    private IDistribution _distribution;
    private int _generatedCount;
    
    public Event? NextEvent(double currentTime)
    {
        var interarrivalTime = _distribution.Generate();
        return new Event
        {
            TimeStamp = currentTime + interarrivalTime,
            Agent = this,
            Type = EventType.Arrival
        };
    }
}
```

#### ServiceAgent (Канал обслуживания)
Обслуживает заявки:

```csharp
public class ServiceAgent : IModellingAgent
{
    private Queue<Request> _queue;
    private List<Channel> _channels;
    
    public void ProcessEvent(double currentTime)
    {
        // Найти свободный канал
        var freeChannel = _channels.FirstOrDefault(c => c.IsFree);
        
        if (freeChannel != null && _queue.Any())
        {
            var request = _queue.Dequeue();
            var serviceTime = _serviceDistribution.Generate();
            freeChannel.StartService(request, currentTime, serviceTime);
        }
    }
}
```

#### QueueAgent (Очередь)
Буферизирует заявки:

```csharp
public class QueueAgent : IModellingAgent
{
    private Queue<Request> _queue;
    private int _capacity;
    
    public bool TryEnqueue(Request request)
    {
        if (_queue.Count < _capacity)
        {
            _queue.Enqueue(request);
            return true;
        }
        return false; // Отказ
    }
}
```

### Контроль точности

Моделирование продолжается до достижения заданной погрешности:

```csharp
public class GenerationErrorSettings
{
    public int GenerationErrorCheckStep { get; set; } = 10_000;
    public int GenerationErrorCheckStepModifier { get; set; } = 3;
    public double MinGenerationError { get; set; } = 0.00001;
}

// Проверка погрешности через регулярные интервалы
if (eventsCount % checkStep == 0)
{
    var error = CalculateKolmogorovDistance();
    if (error < minError)
        break; // Достигнута нужная точность
    
    checkStep *= modifier; // Увеличение интервала проверки
}
```

## Паттерны проектирования

### 1. Repository Pattern

Абстракция доступа к данным:

```csharp
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(string id, CancellationToken ct);
    Task<List<T>> GetAllAsync(CancellationToken ct);
    Task<T> InsertAsync(T entity, CancellationToken ct);
    Task<bool> DeleteAsync(string id, CancellationToken ct);
}
```

### 2. Factory Pattern

Создание агентов по типу:

```csharp
public class AgentFactory : BaseFactory<IModellingAgent>
{
    protected override IModellingAgent CreateInstance(string type, params object[] args)
    {
        return type switch
        {
            "SourceAgent" => new SourceAgent(...),
            "ServiceAgent" => new ServiceAgent(...),
            _ => throw new ArgumentException($"Unknown type: {type}")
        };
    }
}
```

### 3. Strategy Pattern

Различные стратегии распределений:

```csharp
public interface IDistribution
{
    double Generate();
}

public class ExponentialDistribution : IDistribution
{
    private double _lambda;
    public double Generate() => -Math.Log(1 - _random.NextDouble()) / _lambda;
}

public class UniformDistribution : IDistribution
{
    private double _min, _max;
    public double Generate() => _min + (_max - _min) * _random.NextDouble();
}
```

### 4. Observer Pattern

Сбор статистики через наблюдателей:

```csharp
public interface IStatisticObserver
{
    void Update(IModellingAgent agent, double time);
}

public class StatisticCollector : IStatisticObserver
{
    private List<IStatisticObserver> _observers = new();
    
    public void Attach(IStatisticObserver observer)
    {
        _observers.Add(observer);
    }
    
    public void Notify(IModellingAgent agent, double time)
    {
        foreach (var observer in _observers)
            observer.Update(agent, time);
    }
}
```

### 5. Builder Pattern (планируется)

Для создания сложных конфигураций задач:

```csharp
var problem = new ProblemBuilder()
    .WithName("M/M/1 System")
    .AddSource("source", intensity: 0.8)
    .AddService("server", serviceTime: 1.0, channels: 1)
    .AddLink("source", "server")
    .Build();
```

## Работа с данными

### MongoDB Schema

```javascript
// Problems Collection
{
  _id: ObjectId,
  name: String,
  description: String,
  createdAt: ISODate,
  agents: [
    {
      id: String,
      type: String,
      eventTag: String,
      parameters: Object
    }
  ],
  links: [
    {
      from: String,
      to: String
    }
  ],
  generationErrorSettings: {
    generationErrorCheckStep: Number,
    generationErrorCheckStepModifier: Number,
    minGenerationError: Number
  }
}

// Results Collection
{
  _id: ObjectId,
  problemId: ObjectId,
  createdAt: ISODate,
  status: String,
  modelTime: Number,
  eventsProcessed: Number,
  agentStatistics: [
    {
      agentId: String,
      statistics: Object
    }
  ],
  overallStatistics: Object
}
```

### Индексы

```javascript
db.Problems.createIndex({ "name": 1 })
db.Problems.createIndex({ "createdAt": -1 })
db.Results.createIndex({ "problemId": 1, "createdAt": -1 })
```

### Транзакции (если требуется)

```csharp
using var session = await _client.StartSessionAsync();
session.StartTransaction();

try
{
    await _problemRepository.InsertAsync(problem, ct);
    await _resultRepository.InsertAsync(result, ct);
    
    await session.CommitTransactionAsync(ct);
}
catch
{
    await session.AbortTransactionAsync();
    throw;
}
```

## Масштабируемость

### Горизонтальное масштабирование

- **Stateless API**: можно запускать несколько инстансов за load balancer
- **MongoDB Replica Set**: репликация для отказоустойчивости
- **Shared cache**: Redis для кэширования между инстансами

### Оптимизация производительности

1. **Async/Await**: все I/O операции асинхронные
2. **Connection Pooling**: пул соединений к MongoDB
3. **Пагинация**: ограничение размера выборок
4. **Кэширование**: часто используемые данные
5. **Background Jobs**: длительные задачи в фоне

### Обработка длительных задач

Моделирование может занимать много времени. Решения:

```csharp
// Вариант 1: Background Service
public class SimulationBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var task = await _taskQueue.DequeueAsync(ct);
            await ProcessSimulationAsync(task, ct);
        }
    }
}

// Вариант 2: Hangfire/Quartz для job scheduling
[HttpPost("task/run")]
public IActionResult RunTask([FromBody] RunTaskRequest request)
{
    var jobId = BackgroundJob.Enqueue<ISimulationService>(
        x => x.RunSimulationAsync(request.ProblemId, CancellationToken.None));
    
    return Accepted(new { jobId });
}
```

## Безопасность

### Валидация входных данных

```csharp
public class RegisterProblemRequestValidator : AbstractValidator<RegisterProblemRequest>
{
    public RegisterProblemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
            
        RuleFor(x => x.Agents)
            .NotEmpty()
            .Must(HaveUniqueIds);
    }
}
```

### Обработка ошибок

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        var response = exception switch
        {
            ValidationException => CreateValidationError(exception),
            NotFoundException => CreateNotFoundError(exception),
            _ => CreateInternalError(exception)
        };
        
        context.Response.StatusCode = response.StatusCode;
        await context.Response.WriteAsJsonAsync(response, ct);
        return true;
    }
}
```

## Будущие улучшения

1. **Event Sourcing**: сохранение всех событий для воспроизведения
2. **CQRS**: разделение команд и запросов
3. **GraphQL API**: более гибкий API
4. **WebSocket**: real-time обновления
5. **Микросервисы**: разделение на независимые сервисы
6. **Kubernetes**: оркестрация контейнеров

---

Этот документ описывает текущее состояние архитектуры и будет обновляться по мере развития проекта.
