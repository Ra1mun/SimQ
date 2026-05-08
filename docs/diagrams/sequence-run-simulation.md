# Диаграмма последовательности: Запуск и мониторинг моделирования

```mermaid
sequenceDiagram
    participant User as Пользователь
    participant View as View
    participant VM as ViewModel
    participant Client as SimQApiClient
    participant Server as WebAPI (TasksController)
    participant Service as TaskService
    participant Simulation as SimulationModeller
    participant DC as DataCollector

    %% 1. Инициация запуска
    User->>View: Нажатие "Запустить моделирование"
    View->>VM: RunSimulationCommand.Execute()
    VM->>Client: CreateTaskAsync(request)
    Client->>Server: POST /Tasks/v1/tasks
    Server->>Service: AddTask(CreateTaskRequest)
    Service->>Simulation: Запуск моделирования (фоновый поток)
    Service-->>Server: CreateTaskResponse { TaskId }
    Server-->>Client: 200 OK { taskId }
    Client-->>VM: taskId

    %% 2. Циклический опрос состояния
    loop Каждые 750 мс, пока статус ≠ "Выполнено"
        VM->>Client: GetTaskAsync(taskId)
        Client->>Server: GET /Tasks/v1/tasks/{taskId}
        Server->>Service: GetTask(taskId)

        Note right of Simulation: Моделирование выполняется<br/>в фоновом режиме

        Service-->>Server: SimulationTaskDto { Status, CurrentEvents, ModelTime }
        Server-->>Client: 200 OK { status, currentEvents, modelTime }
        Client-->>VM: SimulationTaskDto

        VM->>VM: Обновление свойств:<br/>CurrentEvents, ModelTime
        VM-->>View: PropertyChanged
        View-->>User: Отображение прогресса:<br/>события, модельное время
    end

    %% 3. Моделирование завершено
    Simulation->>DC: BuildResult(endRealTime)
    DC-->>Simulation: SimulationResultData

    %% 4. Получение финальных результатов
    VM->>Client: GetResultsAsync(problemId, taskId)
    Client->>Server: GET /Results/v1/results?taskId={taskId}
    Server->>Service: GetAllResultsAsync(taskId)
    Service-->>Server: ResultListResponse
    Server-->>Client: 200 OK { results[] }
    Client-->>VM: ResultListResponse

    VM->>VM: Обновление результатов
    VM-->>View: PropertyChanged
    View-->>User: Отображение итоговых результатов
```

## Описание этапов

### 1. Инициация запуска моделирования
Пользователь через View инициирует команду запуска. ViewModel отправляет `POST`-запрос на создание задачи. Сервер возвращает уникальный идентификатор (`taskId`) процесса моделирования, переводя задачу в состояние исполнения в фоновом режиме.

### 2. Циклический опрос состояния (Polling)
После получения `taskId` клиент переходит в режим циклического опроса с интервалом **750 мс**. На каждой итерации выполняется `GET`-запрос для получения актуального статуса. Полученные данные — текущее количество событий и модельное время — отображаются в интерфейсе через механизм привязки данных (`INotifyPropertyChanged`).

### 3. Завершение моделирования
Когда `DataCollector.isDone` становится `true` (достигнут лимит событий, модельного времени или ошибки генерации), `SimulationModeller` формирует итоговый результат через `BuildResult()`.

### 4. Получение детальных результатов
Как только статус задачи меняется на "Выполнено", цикл опроса прекращается. Клиент выполняет финальный запрос на получение детальных результатов моделирования из подсистемы сбора данных.
