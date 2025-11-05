# API Документация SimQ

Это полная документация REST API системы SimQ.

## Базовый URL

- **Development**: `http://localhost:5000` или `https://localhost:5001`
- **Production**: `https://your-domain.com`

## Аутентификация

В текущей версии API не требует аутентификации. В будущих версиях планируется добавление JWT токенов.

## Общие параметры

### Формат ответа

Все ответы возвращаются в формате JSON.

### Коды ответов

- `200 OK` — Успешный запрос
- `201 Created` — Ресурс успешно создан
- `204 No Content` — Успешный запрос без содержимого
- `400 Bad Request` — Некорректный запрос
- `404 Not Found` — Ресурс не найден
- `500 Internal Server Error` — Внутренняя ошибка сервера

### Формат ошибок

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["The Name field is required."]
  }
}
```

## Эндпоинты API

### Problems (Задачи моделирования)

#### Получить список всех задач

```http
GET /problems/v1/problems
```

**Ответ**: `200 OK`

```json
{
  "problems": [
    {
      "id": "507f1f77bcf86cd799439011",
      "name": "Задача 1",
      "description": "Описание задачи",
      "createdAt": "2024-11-04T10:00:00Z",
      "agents": [
        {
          "id": "agent1",
          "type": "SourceAgent",
          "eventTag": "source"
        }
      ],
      "generationErrorSettings": {
        "generationErrorCheckStep": 10000,
        "generationErrorCheckStepModifier": 3,
        "minGenerationError": 0.00001
      }
    }
  ],
  "total": 1
}
```

**Статус коды**:
- `200 OK` — Список успешно получен
- `204 No Content` — Нет задач в системе

---

#### Получить задачу по ID

```http
GET /problems/v1/problem/{problemId}
```

**Параметры пути**:
- `problemId` (string, required) — ID задачи

**Ответ**: `200 OK`

```json
{
  "id": "507f1f77bcf86cd799439011",
  "name": "Задача 1",
  "description": "Описание задачи",
  "createdAt": "2024-11-04T10:00:00Z",
  "agents": [
    {
      "id": "agent1",
      "type": "SourceAgent",
      "eventTag": "source",
      "parameters": {
        "intensity": 1.5,
        "distributionType": "Exponential"
      }
    },
    {
      "id": "agent2",
      "type": "ServiceAgent",
      "eventTag": "service",
      "parameters": {
        "serviceTime": 2.0,
        "queueCapacity": 10
      }
    }
  ],
  "links": [
    {
      "fromAgent": "agent1",
      "toAgent": "agent2"
    }
  ],
  "generationErrorSettings": {
    "generationErrorCheckStep": 10000,
    "generationErrorCheckStepModifier": 3,
    "minGenerationError": 0.00001
  }
}
```

**Статус коды**:
- `200 OK` — Задача найдена
- `404 Not Found` — Задача не найдена

---

#### Создать новую задачу

```http
POST /problems/v1/problem
```

**Тело запроса**:

```json
{
  "name": "Новая задача моделирования",
  "description": "Описание задачи",
  "agents": [
    {
      "id": "source1",
      "type": "SourceAgent",
      "eventTag": "arrival",
      "parameters": {
        "intensity": 1.5,
        "distributionType": "Exponential"
      }
    },
    {
      "id": "service1",
      "type": "ServiceAgent",
      "eventTag": "service",
      "parameters": {
        "serviceTime": 2.0,
        "channelCount": 3,
        "queueCapacity": 10,
        "distributionType": "Uniform"
      }
    }
  ],
  "links": [
    {
      "fromAgent": "source1",
      "toAgent": "service1"
    }
  ],
  "generationErrorSettings": {
    "generationErrorCheckStep": 10000,
    "generationErrorCheckStepModifier": 3,
    "minGenerationError": 0.00001
  }
}
```

**Поля запроса**:

- `name` (string, required) — Название задачи
- `description` (string, optional) — Описание задачи
- `agents` (array, required) — Массив агентов СМО
  - `id` (string, required) — Уникальный идентификатор агента
  - `type` (string, required) — Тип агента (SourceAgent, ServiceAgent, etc.)
  - `eventTag` (string, required) — Тег события
  - `parameters` (object, required) — Параметры агента (зависят от типа)
- `links` (array, required) — Связи между агентами
  - `fromAgent` (string) — ID агента-источника
  - `toAgent` (string) — ID агента-получателя
- `generationErrorSettings` (object, optional) — Настройки точности моделирования
  - `generationErrorCheckStep` (integer) — Шаг проверки погрешности
  - `generationErrorCheckStepModifier` (integer) — Модификатор шага
  - `minGenerationError` (double) — Минимальная погрешность

**Ответ**: `201 Created`

```json
{
  "problemId": "507f1f77bcf86cd799439011",
  "name": "Новая задача моделирования",
  "createdAt": "2024-11-04T10:00:00Z",
  "message": "Problem successfully registered"
}
```

**Статус коды**:
- `201 Created` — Задача успешно создана
- `400 Bad Request` — Некорректные данные запроса

---

#### Удалить задачу

```http
DELETE /problems/v1/problem/{problemId}
```

**Параметры пути**:
- `problemId` (string, required) — ID задачи

**Ответ**: `200 OK`

```json
{
  "message": "Problem successfully deleted"
}
```

**Статус коды**:
- `200 OK` — Задача успешно удалена
- `404 Not Found` — Задача не найдена

---

#### Получить результаты задачи

```http
GET /problems/v1/problem/{problemId}/results
```

**Параметры пути**:
- `problemId` (string, required) — ID задачи

**Ответ**: `200 OK`

```json
{
  "problemId": "507f1f77bcf86cd799439011",
  "results": [
    {
      "id": "607f1f77bcf86cd799439012",
      "createdAt": "2024-11-04T11:00:00Z",
      "status": "Completed",
      "modelTime": 100000.0,
      "eventsProcessed": 50000,
      "statistics": {
        "averageQueueLength": 2.5,
        "systemUtilization": 0.75,
        "averageWaitTime": 5.2
      }
    }
  ],
  "total": 1
}
```

**Статус коды**:
- `200 OK` — Результаты получены
- `404 Not Found` — Задача не найдена

---

#### Получить конкретный результат

```http
GET /problems/v1/problem/{problemId}/result/{resultId}
```

**Параметры пути**:
- `problemId` (string, required) — ID задачи
- `resultId` (string, required) — ID результата

**Ответ**: `200 OK`

```json
{
  "id": "607f1f77bcf86cd799439012",
  "problemId": "507f1f77bcf86cd799439011",
  "createdAt": "2024-11-04T11:00:00Z",
  "status": "Completed",
  "modelTime": 100000.0,
  "eventsProcessed": 50000,
  "agentStatistics": [
    {
      "agentId": "source1",
      "eventsGenerated": 50000,
      "averageInterarrivalTime": 2.0
    },
    {
      "agentId": "service1",
      "eventsProcessed": 49500,
      "eventsRejected": 500,
      "averageServiceTime": 2.1,
      "utilization": 0.75,
      "averageQueueLength": 2.5
    }
  ],
  "statistics": {
    "averageQueueLength": 2.5,
    "systemUtilization": 0.75,
    "averageWaitTime": 5.2,
    "rejectionProbability": 0.01
  }
}
```

**Статус коды**:
- `200 OK` — Результат найден
- `404 Not Found` — Результат не найден

---

#### Удалить результат

```http
DELETE /problems/v1/problem/{problemId}/result/{resultId}
```

**Параметры пути**:
- `problemId` (string, required) — ID задачи
- `resultId` (string, required) — ID результата

**Ответ**: `200 OK`

**Статус коды**:
- `200 OK` — Результат удален
- `404 Not Found` — Результат не найден

---

### Tasks (Процессы выполнения)

#### Получить список всех процессов

```http
GET /tasks/v1/tasks
```

**Ответ**: `200 OK`

```json
{
  "tasks": [
    {
      "id": "task-uuid-1",
      "problemId": "507f1f77bcf86cd799439011",
      "problemName": "Задача 1",
      "status": "Running",
      "startedAt": "2024-11-04T12:00:00Z",
      "progress": 45.5
    },
    {
      "id": "task-uuid-2",
      "problemId": "507f1f77bcf86cd799439022",
      "problemName": "Задача 2",
      "status": "Completed",
      "startedAt": "2024-11-04T11:00:00Z",
      "completedAt": "2024-11-04T11:30:00Z",
      "resultId": "607f1f77bcf86cd799439012"
    }
  ],
  "total": 2
}
```

**Возможные статусы**:
- `Pending` — Ожидает запуска
- `Running` — Выполняется
- `Completed` — Завершено успешно
- `Failed` — Ошибка выполнения
- `Cancelled` — Отменено

**Статус коды**:
- `200 OK` — Список получен
- `204 No Content` — Нет процессов

---

#### Запустить выполнение задачи

```http
POST /tasks/v1/task/run
```

**Тело запроса**:

```json
{
  "problemId": "507f1f77bcf86cd799439011",
  "parameters": {
    "maxModelTime": 100000.0,
    "maxEvents": 50000,
    "seed": 12345
  }
}
```

**Поля запроса**:

- `problemId` (string, required) — ID задачи для запуска
- `parameters` (object, optional) — Параметры запуска
  - `maxModelTime` (double, optional) — Максимальное модельное время
  - `maxEvents` (integer, optional) — Максимальное количество событий
  - `seed` (integer, optional) — Зерно для генератора случайных чисел

**Ответ**: `202 Accepted`

```json
{
  "taskId": "task-uuid-1",
  "problemId": "507f1f77bcf86cd799439011",
  "status": "Pending",
  "message": "Task queued for execution"
}
```

**Статус коды**:
- `202 Accepted` — Задача принята к выполнению
- `404 Not Found` — Задача не найдена
- `400 Bad Request` — Некорректные параметры

---

#### Получить статус процесса

```http
GET /tasks/v1/task/{taskId}
```

**Параметры пути**:
- `taskId` (string, required) — ID процесса

**Ответ**: `200 OK`

```json
{
  "id": "task-uuid-1",
  "problemId": "507f1f77bcf86cd799439011",
  "problemName": "Задача 1",
  "status": "Running",
  "startedAt": "2024-11-04T12:00:00Z",
  "progress": 45.5,
  "currentModelTime": 45500.0,
  "eventsProcessed": 22750,
  "estimatedTimeRemaining": "00:15:30"
}
```

**Статус коды**:
- `200 OK` — Статус получен
- `404 Not Found` — Процесс не найден

---

#### Отменить выполнение процесса

```http
POST /tasks/v1/task/{taskId}/cancel
```

**Параметры пути**:
- `taskId` (string, required) — ID процесса

**Ответ**: `200 OK`

```json
{
  "taskId": "task-uuid-1",
  "status": "Cancelled",
  "message": "Task cancelled successfully"
}
```

**Статус коды**:
- `200 OK` — Процесс отменен
- `404 Not Found` — Процесс не найден
- `400 Bad Request` — Процесс уже завершен

---

## Типы агентов

### SourceAgent (Источник заявок)

Генерирует заявки согласно заданному распределению.

**Параметры**:
```json
{
  "id": "source1",
  "type": "SourceAgent",
  "eventTag": "arrival",
  "parameters": {
    "intensity": 1.5,
    "distributionType": "Exponential"
  }
}
```

- `intensity` (double) — Интенсивность потока заявок
- `distributionType` (string) — Тип распределения: `Exponential`, `Uniform`, `Normal`

### ServiceAgent (Канал обслуживания)

Обслуживает поступающие заявки.

**Параметры**:
```json
{
  "id": "service1",
  "type": "ServiceAgent",
  "eventTag": "service",
  "parameters": {
    "serviceTime": 2.0,
    "channelCount": 3,
    "queueCapacity": 10,
    "distributionType": "Uniform"
  }
}
```

- `serviceTime` (double) — Среднее время обслуживания
- `channelCount` (integer) — Количество каналов обслуживания
- `queueCapacity` (integer) — Вместимость очереди
- `distributionType` (string) — Тип распределения времени обслуживания

### QueueAgent (Очередь)

Буферизирует заявки.

**Параметры**:
```json
{
  "id": "queue1",
  "type": "QueueAgent",
  "eventTag": "queue",
  "parameters": {
    "capacity": 20,
    "discipline": "FIFO"
  }
}
```

- `capacity` (integer) — Вместимость очереди
- `discipline` (string) — Дисциплина обслуживания: `FIFO`, `LIFO`, `Priority`

### SinkAgent (Приемник)

Завершающий узел, собирает статистику.

**Параметры**:
```json
{
  "id": "sink1",
  "type": "SinkAgent",
  "eventTag": "departure",
  "parameters": {}
}
```

---

## Примеры использования

### Пример 1: Простая система M/M/1

Создание простой системы массового обслуживания с одним источником, одним каналом обслуживания и неограниченной очередью.

```bash
curl -X POST http://localhost:5000/problems/v1/problem \
  -H "Content-Type: application/json" \
  -d '{
    "name": "M/M/1 System",
    "description": "Simple single-server queue",
    "agents": [
      {
        "id": "source",
        "type": "SourceAgent",
        "eventTag": "arrival",
        "parameters": {
          "intensity": 0.8,
          "distributionType": "Exponential"
        }
      },
      {
        "id": "server",
        "type": "ServiceAgent",
        "eventTag": "service",
        "parameters": {
          "serviceTime": 1.0,
          "channelCount": 1,
          "queueCapacity": 999999,
          "distributionType": "Exponential"
        }
      },
      {
        "id": "sink",
        "type": "SinkAgent",
        "eventTag": "departure",
        "parameters": {}
      }
    ],
    "links": [
      {"fromAgent": "source", "toAgent": "server"},
      {"fromAgent": "server", "toAgent": "sink"}
    ],
    "generationErrorSettings": {
      "generationErrorCheckStep": 10000,
      "generationErrorCheckStepModifier": 3,
      "minGenerationError": 0.00001
    }
  }'
```

### Пример 2: Запуск моделирования

```bash
curl -X POST http://localhost:5000/tasks/v1/task/run \
  -H "Content-Type: application/json" \
  -d '{
    "problemId": "507f1f77bcf86cd799439011",
    "parameters": {
      "maxModelTime": 100000.0,
      "seed": 12345
    }
  }'
```

### Пример 3: Получение результатов

```bash
curl -X GET http://localhost:5000/problems/v1/problem/507f1f77bcf86cd799439011/results
```

---

## Swagger UI

Интерактивная документация доступна в Swagger UI:

```
https://localhost:5001/swagger
```

В Swagger UI вы можете:
- Просматривать все эндпоинты
- Тестировать API запросы
- Просматривать схемы моделей
- Скачать OpenAPI спецификацию

---

## WebSocket поддержка (планируется)

В будущих версиях планируется добавление WebSocket для:
- Real-time обновления статуса выполнения задач
- Потоковая передача статистики в процессе моделирования
- Уведомления о завершении задач

---

## Версионирование API

API использует версионирование через URL:
- `/problems/v1/...`
- `/tasks/v1/...`

При изменении API будут добавляться новые версии с сохранением обратной совместимости.

---

## Лимиты и квоты

В текущей версии лимиты отсутствуют. В будущем планируется добавление:
- Rate limiting (ограничение количества запросов)
- Таймауты на выполнение задач
- Ограничения на размер задач

---

## Поддержка

Для вопросов и сообщений об ошибках создавайте issue в репозитории проекта.
