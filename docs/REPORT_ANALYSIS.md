# Анализ проекта SimQ для отчёта Соболя М.В.

---

## 1. Структуры данных (DTO) — «Что передаём?»

### Доменная модель (`SimQ.Domain/Models/ResultAggregation/`)

**`Result.cs`** — основная сущность результата (MongoDB):
- `Id`, `ProblemId`, `ProblemName`, `TaskId` — идентификаторы
- `Text` — текстовое представление результатов
- `Data` — вложенный объект `SimulationResultData`
- `CreatedAt`, `UpdatedAt`

**`SimulationResultData.cs`** — данные моделирования:
- `EndRealTime`, `MaxRealTime` — реальное время
- `CurrentEventsAmount`, `MaxEventsAmount` — кол-во событий
- `CurrentModelationTime`, `MaxModelationTime` — модельное время
- `CurrentGenerationError`, `MinGenerationError` — погрешность
- `TotalCalls` — общее число обращений
- `AgentResults` (`List<AgentStatisticResult>`) — статистика по агентам

**`AgentStatisticResult.cs`** — статистика одного агента:
- `AgentId`, `AgentType` (SOURCE / SERVICE_BLOCK / BUFFER / ORBIT)
- `Average` — среднее значение
- `StatesProbabilities` (`Dictionary<string, double>`) — эмпирическое распределение (состояние → вероятность)

### DTO на уровне API (`SimQ.Core/Dtos/Out/`)
`ResultDto` и `SimulationResultDataDto` — зеркальные копии доменных моделей без MongoDB-атрибутов. `AgentStatisticResultDto` аналогично.

### DTO на клиенте (`SimQ.Client/Services/ApiDtos.cs`)
Клиент имеет собственные зеркальные классы: `ResultDto`, `SimulationResultDataDto`, `AgentStatisticResultDto` — десериализуются из JSON-ответов API.

---

## 2. Логика сбора в Ядре — «Как фиксируем?»

### `StatisticCollector` (`SimQ.Core/Statistic/StatisticCollector.cs`)
Класс-коллектор статистики. Метод `CollectStatistic(DataCollector data)`:
1. Принимает объект `DataCollector` (содержит `agentsStatisticData` — словарь агент → состояния)
2. Создаёт `StatesStatistic`, который **нормализует** сырые данные (делит время пребывания в каждом состоянии на общее модельное время → получает вероятности)
3. Вычисляет средние значения (`CalculateAverages`) по формуле: среднее = сумма(i * P(i))

### `StatesStatistic` (`SimQ.Core/Statistic/StatesStatistic.cs`)
- Нормализация: `states[agent][i] /= totalTime`
- Методы `Get_EmpDist`, `EmpDistToString` для получения эмпирической функции распределения

**Паттерн**: Не классический Observer. `DataCollector` — это агрегатор, который накапливает данные в процессе моделирования, а `StatisticCollector` обрабатывает их постфактум.

---

## 3. API Эндпоинты — «Как клиент забирает данные?»

### `ResultsController` (`SimQ.WebApi/Controllers/ResultsController.cs`)

| Метод | Маршрут | Описание |
|-------|---------|----------|
| `GET` | `/Results/v1/results?problemId=&taskId=` | Список результатов (фильтр по задаче/проблеме) |
| `GET` | `/Results/v1/result/{resultId}` | Один результат по ID |
| `DELETE` | `/Results/v1/result/{resultId}` | Удалить результат |

Формат ответа — **JSON**. Данные возвращаются **целиком после завершения** моделирования (нет порционной отдачи). Во время моделирования клиент поллит статус задачи через `GET /Tasks/task/{taskId}`, где в `ResultData` приходят промежуточные показатели прогресса (`CurrentEventsAmount`, `CurrentModelationTime`).

Сервис `ResultService` использует **AutoMapper** для маппинга `Result → ResultDto`.

---

## 4. Логика обработки на клиенте (ViewModel)

### `MainViewModel` (`SimQ.Client/ViewModels/MainViewModel.cs`)

**Запуск симуляции** — метод `StartSimulationAsync()`:
1. Вызывает `_api.CreateTaskAsync(...)` с `ProblemId` и `MaxSteps`
2. В цикле **поллит** `_api.GetTaskAsync(taskId)` каждые 750 мс
3. Обновляет `Progress` через `MapProgress()` — вычисляет прогресс как max(currentEvents/maxEvents, currentTime/maxTime)
4. Завершает при статусе `Completed` / `Error` / `Canceled`

**Отображение результатов** — `ResultTable` (`ObservableCollection<ResultRow>`):
- Заполняется из `SampleData.CreateResultTable()` — **на стороне клиента** генерируется распределение P(N) по гауссовой формуле (среднее 6.3, σ = 2.4), вычисляются CDF и ширины столбцов для гистограммы
- Доп. расчёты: `BarWidth = 220 * (P / maxP)`, `ChartHeight = 200 * (P / maxP)`

**Загрузка истории** — `RefreshHistoryAsync()` запрашивает `_api.GetTasksAsync()` и маппит статусы в `RunHistory`.

---

## 5. Библиотеки визуализации — «Как рисуем?»

**Внешних библиотек для графиков НЕТ.** В `SimQ.Client.csproj` присутствуют только:
- `Avalonia 11.2.1` (UI-фреймворк)
- `CommunityToolkit.Mvvm 8.3.2` (MVVM)

Графики реализованы **вручную** на чистом Avalonia XAML:
- Таблица распределения P(N) — через `ItemsControl` + `DataTemplate`
- Гистограмма — через `UniformGrid` + `Border` с привязкой `Height="{Binding ChartHeight}"` (высота столбца пропорциональна вероятности)
- Горизонтальные бары — `Border` с `Width="{Binding BarWidth}"`

Никакие LiveCharts2, OxyPlot и т.п. **не используются**.

---

## 6. Скриншоты интерфейса

По XAML-разметке `ResultsView.axaml` экран результатов содержит:
- **Заголовок**: название задачи, номер запуска, кол-во итераций, seed
- **Кнопки экспорта**: CSV, JSON, PDF
- **Таблицу распределения P(N)** с горизонтальными барами
- **Столбчатую диаграмму** P(N)
