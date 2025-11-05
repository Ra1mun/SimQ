# Руководство по разработке SimQ

Это руководство содержит информацию для разработчиков, работающих над проектом SimQ.

## Оглавление

- [Настройка окружения разработки](#настройка-окружения-разработки)
- [Архитектура проекта](#архитектура-проекта)
- [Работа с кодовой базой](#работа-с-кодовой-базой)
- [Тестирование](#тестирование)
- [Отладка](#отладка)
- [Лучшие практики](#лучшие-практики)

## Настройка окружения разработки

### Backend разработка

1. **Установите .NET 8.0 SDK**
   ```bash
   # Проверка версии
   dotnet --version
   ```

2. **Установите MongoDB**
   - Windows: используйте MongoDB Community Edition
   - Linux/macOS: используйте менеджер пакетов или Docker

3. **IDE для разработки**
   - Visual Studio 2022 (рекомендуется)
   - JetBrains Rider
   - VS Code с расширением C#

4. **Настройте локальную конфигурацию**
   
   Создайте `SimQ.WebApi/appsettings.Development.json`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "DatabaseSettings": {
       "ConnectionString": "mongodb://localhost:27017",
       "DatabaseName": "SimQDatabase_Dev"
     },
     "AllowedOrigins": [
       "http://localhost:3000"
     ]
   }
   ```

### Frontend разработка

1. **Установите Node.js и npm**
   ```bash
   node --version  # v16 или выше
   npm --version
   ```

2. **Установите зависимости**
   ```bash
   cd client
   npm install
   ```

3. **Настройте переменные окружения**
   
   Создайте `client/.env.local`:
   ```
   REACT_APP_API_URL=http://localhost:5000
   ```

## Архитектура проекта

### Обзор слоев

#### 1. SimQ.Domain
**Назначение**: Содержит доменные модели (Entity классы) для базы данных.

**Зависимости**: Нет зависимостей от других проектов.

**Пример**:
```csharp
namespace SimQ.Domain.Models.ProblemAggregation
{
    public class ProblemEntity
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        // ...
    }
}
```

#### 2. SimQ.DAL (Data Access Layer)
**Назначение**: Работа с базой данных через репозитории.

**Зависимости**: SimQ.Domain

**Паттерны**:
- Repository Pattern
- Unit of Work (если применимо)

**Пример репозитория**:
```csharp
public interface IProblemRepository
{
    Task<ProblemEntity?> GetByIdAsync(string id, CancellationToken ct);
    Task<List<ProblemEntity>> GetAllAsync(CancellationToken ct);
    Task<ProblemEntity> InsertAsync(ProblemEntity entity, CancellationToken ct);
    Task<bool> DeleteAsync(string id, CancellationToken ct);
}
```

#### 3. SimQ.Core
**Назначение**: Бизнес-логика приложения.

**Зависимости**: SimQ.Domain, SimQ.DAL

**Ключевые компоненты**:

- **Models**: Внутренние модели для работы системы моделирования
- **Services**: Бизнес-сервисы (ProblemService, TasksService)
- **Modeller**: Модуль моделирования СМО
  - `Supervisor` — диспетчер агентов
  - `IModellingAgent` — интерфейс агента
- **Statistic**: Сбор и обработка статистики
- **Factories**: Создание агентов и распределений
- **Convertors**: Конвертация между моделями

#### 4. SimQ.WebApi
**Назначение**: REST API для взаимодействия с клиентами.

**Зависимости**: SimQ.Core, SimQ.DAL, SimQ.Domain

**Структура**:
- Controllers: API контроллеры
- DTOs: Объекты передачи данных (входящие/исходящие)
- Middleware: Обработка ошибок, логирование

#### 5. Client (Frontend)
**Назначение**: Веб-интерфейс пользователя.

**Технологии**: React, TypeScript, Material-UI

**Структура**:
```
src/
├── domain/         # Типы, интерфейсы, enums
├── services/       # API клиенты
├── widgets/        # React компоненты
│   ├── ProblemsTab/
│   ├── TasksTab/
│   └── AgentsTab/
├── App.tsx
└── index.tsx
```

### Поток данных

```
User Input → Controller → Service → Repository → Database
                ↓            ↓           ↓
              DTOs    ← Mapping ← Entity Models
```

## Работа с кодовой базой

### Структура решения (Solution)

```
SimQ.sln
├── SimQ.Domain        # Доменные модели
├── SimQ.DAL           # Доступ к данным
├── SimQ.Core          # Бизнес-логика
├── SimQ.WebApi        # REST API
├── SimQ.Simulation    # Модуль симуляций
└── SimQ.Tests         # Тесты
```

### Dependency Injection

Все зависимости регистрируются в `Program.cs` через extension методы:

```csharp
services
    .AddDatabase(configuration)        // Регистрация MongoDB
    .AddFactories()                   // Фабрики
    .AddConverters()                  // Конверторы
    .AddRepositories()                // Репозитории
    .AddServices();                   // Сервисы
```

Каждый проект имеет свой `DependencyInjection.cs` для регистрации зависимостей.

### Работа с агентами моделирования

#### Интерфейс IModellingAgent

Все агенты реализуют базовый интерфейс:

```csharp
public interface IModellingAgent
{
    string Id { get; }
    string EventTag { get; }
    int GetCurrentState();
    Event? NextEvent(double currentTime);
    void ProcessEvent(double currentTime);
}
```

#### Создание нового типа агента

1. **Создайте класс агента** в `SimQ.Core/Models/`:

```csharp
public class MyCustomAgent : IModellingAgent
{
    public string Id { get; set; }
    public string EventTag { get; set; }
    
    public Event? NextEvent(double currentTime)
    {
        // Логика генерации следующего события
    }
    
    public void ProcessEvent(double currentTime)
    {
        // Обработка события
    }
    
    public int GetCurrentState()
    {
        // Возврат текущего состояния
    }
}
```

2. **Добавьте в фабрику** (`SimQ.Core/Factories/AgentFactory.cs`):

```csharp
public class AgentFactory : BaseFactory<IModellingAgent>
{
    protected override IModellingAgent CreateInstance(string type, params object[] args)
    {
        return type switch
        {
            "MyCustomAgent" => new MyCustomAgent(/* параметры */),
            // другие типы...
            _ => throw new ArgumentException($"Unknown agent type: {type}")
        };
    }
}
```

3. **Создайте DTO** в `SimQ.Core/Dtos/`:

```csharp
public class MyCustomAgentDto
{
    public string Id { get; set; }
    public string Type { get; set; } = "MyCustomAgent";
    // Дополнительные параметры
}
```

4. **Обновите конвертор** в `SimQ.Core/Convertors/`:

```csharp
public class AgentConvertor
{
    public IModellingAgent Convert(AgentDtoBase dto)
    {
        return dto switch
        {
            MyCustomAgentDto customDto => ConvertMyCustomAgent(customDto),
            // другие типы...
            _ => throw new NotSupportedException()
        };
    }
}
```

### Работа с сервисами

Сервисы содержат бизнес-логику и используют репозитории для доступа к данным.

**Пример сервиса**:

```csharp
public interface IProblemService
{
    Task<ProblemResponse?> GetProblemAsync(string id, CancellationToken ct);
    Task<RegisterProblemResponse?> RegisterProblem(RegisterProblemRequest request, CancellationToken ct);
}

internal class ProblemService : IProblemService
{
    private readonly IProblemRepository _repository;
    private readonly IMapper _mapper;
    
    public ProblemService(IProblemRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }
    
    public async Task<ProblemResponse?> GetProblemAsync(string id, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        return _mapper.Map<ProblemResponse>(entity);
    }
}
```

### AutoMapper профили

Маппинг между Entity и DTO настраивается в профилях:

```csharp
// SimQ.Core/MappingProfiles/ProblemProfile.cs
public class ProblemProfile : Profile
{
    public ProblemProfile()
    {
        CreateMap<ProblemEntity, ProblemResponse>();
        CreateMap<RegisterProblemRequest, ProblemEntity>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow));
    }
}
```

## Тестирование

### Unit тесты

Тесты находятся в проекте `SimQ.Tests`.

**Структура тестов**:
```
SimQ.Tests/
├── Core/
│   ├── Services/
│   │   └── ProblemServiceTests.cs
│   └── Models/
├── WebApi/
│   └── Controllers/
└── Helpers/
```

**Пример теста**:

```csharp
public class ProblemServiceTests
{
    private readonly Mock<IProblemRepository> _repositoryMock;
    private readonly IProblemService _service;
    
    public ProblemServiceTests()
    {
        _repositoryMock = new Mock<IProblemRepository>();
        var mapper = new MapperConfiguration(cfg => 
            cfg.AddProfile<ProblemProfile>()).CreateMapper();
        
        _service = new ProblemService(_repositoryMock.Object, mapper);
    }
    
    [Fact]
    public async Task GetProblemAsync_ValidId_ReturnsProblem()
    {
        // Arrange
        var problemId = "123";
        var entity = new ProblemEntity { Id = ObjectId.Parse(problemId) };
        _repositoryMock.Setup(r => r.GetByIdAsync(problemId, default))
            .ReturnsAsync(entity);
        
        // Act
        var result = await _service.GetProblemAsync(problemId, default);
        
        // Assert
        Assert.NotNull(result);
    }
}
```

### Запуск тестов

```bash
# Все тесты
dotnet test

# Конкретный проект
dotnet test SimQ.Tests/SimQ.Tests.csproj

# С coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Отладка

### Backend отладка

**В Visual Studio/Rider**:
1. Установите точки останова (breakpoints)
2. Нажмите F5 для запуска в режиме отладки
3. Используйте окна Locals, Watch для просмотра переменных

**Логирование**:
```csharp
public class ProblemService
{
    private readonly ILogger<ProblemService> _logger;
    
    public async Task<ProblemResponse?> GetProblemAsync(string id, CancellationToken ct)
    {
        _logger.LogDebug("Getting problem with id: {ProblemId}", id);
        // ...
    }
}
```

### Frontend отладка

**React DevTools**:
- Установите расширение React Developer Tools для браузера
- Инспектируйте компоненты и их состояние

**Browser DevTools**:
- Console: проверка ошибок и логов
- Network: мониторинг HTTP запросов
- Sources: отладка TypeScript кода

**VS Code отладка**:

Создайте `.vscode/launch.json`:
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "type": "chrome",
      "request": "launch",
      "name": "Launch Chrome",
      "url": "http://localhost:3000",
      "webRoot": "${workspaceFolder}/client/src"
    }
  ]
}
```

## Лучшие практики

### Код-стайл

**C#**:
- Используйте PascalCase для публичных членов
- camelCase для приватных полей
- Префикс `_` для приватных полей
- Async методы должны иметь суффикс `Async`
- Используйте `var` когда тип очевиден

**TypeScript/React**:
- PascalCase для компонентов
- camelCase для функций и переменных
- Используйте функциональные компоненты с хуками
- Типизируйте все пропсы и состояния

### Принципы

1. **SOLID принципы**
   - Single Responsibility: один класс — одна ответственность
   - Open/Closed: открыт для расширения, закрыт для модификации
   - Liskov Substitution: подтипы должны заменять базовые типы
   - Interface Segregation: много специализированных интерфейсов
   - Dependency Inversion: зависимость от абстракций

2. **DRY (Don't Repeat Yourself)**
   - Избегайте дублирования кода
   - Выносите общую логику в отдельные методы/функции

3. **YAGNI (You Aren't Gonna Need It)**
   - Не добавляйте функциональность "на будущее"
   - Реализуйте только то, что нужно сейчас

### Git workflow

1. **Создайте ветку для фичи**:
   ```bash
   git checkout -b feature/my-new-feature
   ```

2. **Делайте частые коммиты**:
   ```bash
   git add .
   git commit -m "feat: add new agent type"
   ```

3. **Формат коммитов** (Conventional Commits):
   - `feat:` — новая функциональность
   - `fix:` — исправление бага
   - `docs:` — изменения в документации
   - `refactor:` — рефакторинг кода
   - `test:` — добавление тестов
   - `chore:` — рутинные задачи

4. **Создайте Pull Request**:
   - Опишите что изменилось
   - Добавьте скриншоты (если применимо)
   - Упомяните связанные issues

### Безопасность

- **Не коммитьте секреты**: пароли, ключи API, строки подключения
- Используйте User Secrets для локальной разработки:
  ```bash
  dotnet user-secrets set "DatabaseSettings:ConnectionString" "mongodb://..."
  ```
- Валидируйте входные данные в контроллерах
- Используйте HTTPS в продакшене

### Производительность

- Используйте `async/await` для I/O операций
- Избегайте блокирующих вызовов
- Используйте пагинацию для больших списков
- Кешируйте часто используемые данные
- Оптимизируйте запросы к БД (индексы, проекции)

## Полезные команды

### .NET

```bash
# Сборка решения
dotnet build

# Запуск проекта
dotnet run --project SimQ.WebApi

# Создание миграции (если используется EF Core)
dotnet ef migrations add MigrationName

# Очистка артефактов сборки
dotnet clean

# Восстановление пакетов
dotnet restore
```

### npm

```bash
# Установка зависимостей
npm install

# Запуск dev сервера
npm start

# Сборка для продакшена
npm run build

# Проверка типов TypeScript
npx tsc --noEmit
```

### MongoDB

```bash
# Подключение к БД
mongosh mongodb://localhost:27017/SimQDatabase

# Экспорт данных
mongoexport --db=SimQDatabase --collection=Problems --out=problems.json

# Импорт данных
mongoimport --db=SimQDatabase --collection=Problems --file=problems.json
```

## Дополнительные ресурсы

- [ASP.NET Core документация](https://docs.microsoft.com/aspnet/core)
- [React документация](https://react.dev)
- [MongoDB .NET Driver](https://mongodb.github.io/mongo-csharp-driver/)
- [Material-UI документация](https://mui.com)

---

Если у вас есть вопросы или предложения по улучшению этого руководства, создайте issue или pull request.
