# Часто задаваемые вопросы (FAQ)

## Оглавление

- [Общие вопросы](#общие-вопросы)
- [Установка и настройка](#установка-и-настройка)
- [Использование](#использование)
- [Разработка](#разработка)
- [Устранение неполадок](#устранение-неполадок)
- [API](#api)
- [Производительность](#производительность)

## Общие вопросы

### Что такое SimQ?

SimQ — это система для моделирования и анализа систем массового обслуживания (СМО). Она позволяет создавать модели различных систем обслуживания, запускать симуляции и анализировать результаты.

### Для кого предназначен SimQ?

- Студенты, изучающие теорию массового обслуживания
- Исследователи, работающие с моделями очередей
- Инженеры, проектирующие системы обслуживания
- Аналитики, оптимизирующие производительность систем

### Под какой лицензией распространяется SimQ?

Проект разработан для образовательных целей ТГУ и не является открытым программным обеспечением. См. [LICENSE](LICENSE) для деталей.

### Какие типы СМО поддерживаются?

- M/M/1 — одноканальная система с экспоненциальными распределениями
- M/M/c — многоканальная система
- M/G/1 — система с произвольным распределением обслуживания
- Сети массового обслуживания
- Системы с ограниченной очередью
- Системы с отказами

## Установка и настройка

### Какие требования для запуска SimQ?

**Backend**:
- .NET SDK 8.0 или выше
- MongoDB 4.0 или выше

**Frontend**:
- Node.js 16 или выше
- npm или yarn

**Операционная система**: Windows, Linux, macOS

### Как установить MongoDB?

**Windows**:
```powershell
# Скачайте установщик с mongodb.com
# Или используйте Chocolatey
choco install mongodb
```

**Linux (Ubuntu/Debian)**:
```bash
wget -qO - https://www.mongodb.org/static/pgp/server-6.0.asc | sudo apt-key add -
echo "deb [ arch=amd64,arm64 ] https://repo.mongodb.org/apt/ubuntu jammy/mongodb-org/6.0 multiverse" | sudo tee /etc/apt/sources.list.d/mongodb-org-6.0.list
sudo apt-get update
sudo apt-get install -y mongodb-org
sudo systemctl start mongod
```

**macOS**:
```bash
brew tap mongodb/brew
brew install mongodb-community
brew services start mongodb-community
```

**Docker**:
```bash
docker run -d -p 27017:27017 --name simq-mongo mongo:latest
```

### Как изменить порт API?

Измените в `SimQ.WebApi/appsettings.json` или через переменные окружения:

```bash
# Linux/macOS
export ASPNETCORE_URLS="http://localhost:8080"

# Windows PowerShell
$env:ASPNETCORE_URLS="http://localhost:8080"
```

Или в `launchSettings.json`:
```json
{
  "profiles": {
    "SimQ.WebApi": {
      "applicationUrl": "http://localhost:8080"
    }
  }
}
```

### Как настроить подключение к удаленной MongoDB?

Измените строку подключения в `appsettings.json`:

```json
{
  "DatabaseSettings": {
    "ConnectionString": "mongodb://username:password@remote-host:27017",
    "DatabaseName": "SimQDatabase"
  }
}
```

Или используйте переменные окружения:
```bash
export DatabaseSettings__ConnectionString="mongodb://username:password@remote-host:27017"
export DatabaseSettings__DatabaseName="SimQDatabase"
```

## Использование

### Как создать простую систему M/M/1?

**Через API**:
```bash
curl -X POST http://localhost:5000/problems/v1/problem \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Simple M/M/1",
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
      }
    ],
    "links": [
      {"fromAgent": "source", "toAgent": "server"}
    ]
  }'
```

**Через веб-интерфейс**:
1. Откройте http://localhost:3000
2. Перейдите на вкладку "Задачи"
3. Нажмите "Создать новую задачу"
4. Добавьте агенты и связи
5. Сохраните задачу

### Как запустить моделирование?

```bash
# Получите ID задачи
curl http://localhost:5000/problems/v1/problems

# Запустите моделирование
curl -X POST http://localhost:5000/tasks/v1/task/run \
  -H "Content-Type: application/json" \
  -d '{
    "problemId": "YOUR_PROBLEM_ID",
    "parameters": {
      "maxModelTime": 100000.0,
      "seed": 12345
    }
  }'
```

### Как посмотреть результаты?

```bash
# Получить все результаты задачи
curl http://localhost:5000/problems/v1/problem/{problemId}/results

# Получить конкретный результат
curl http://localhost:5000/problems/v1/problem/{problemId}/result/{resultId}
```

### Какие метрики собираются?

Для каждого агента:
- Количество обработанных заявок
- Количество отказов
- Среднее время обслуживания
- Коэффициент использования
- Средняя длина очереди

Общие метрики:
- Средняя длина очереди в системе
- Среднее время ожидания
- Вероятность отказа
- Пропускная способность

### Как настроить точность моделирования?

В параметрах задачи укажите:

```json
{
  "generationErrorSettings": {
    "generationErrorCheckStep": 10000,
    "generationErrorCheckStepModifier": 3,
    "minGenerationError": 0.00001
  }
}
```

- `generationErrorCheckStep` — начальный интервал проверки (в событиях)
- `generationErrorCheckStepModifier` — множитель для увеличения интервала
- `minGenerationError` — минимальная допустимая погрешность

## Разработка

### Как добавить новый тип агента?

1. Создайте класс в `SimQ.Core/Models/`:
```csharp
public class MyAgent : IModellingAgent
{
    // реализация
}
```

2. Добавьте в фабрику `AgentFactory`
3. Создайте DTO в `SimQ.Core/Dtos/`
4. Обновите конвертор

См. [DEVELOPMENT.md](docs/DEVELOPMENT.md#создание-нового-типа-агента) для деталей.

### Как запустить тесты?

```bash
# Все тесты
dotnet test

# С подробным выводом
dotnet test --logger "console;verbosity=detailed"

# С coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Как включить детальное логирование?

В `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "SimQ": "Trace",
      "Microsoft": "Information"
    }
  }
}
```

### Как отладить frontend?

**VS Code**:
1. Установите расширение "Debugger for Chrome"
2. Запустите `npm start`
3. Нажмите F5 в VS Code

**Browser DevTools**:
1. Откройте DevTools (F12)
2. Перейдите на вкладку Sources
3. Установите breakpoints
4. React DevTools для инспекции компонентов

## Устранение неполадок

### Backend не запускается

**Проблема**: `Unable to connect to MongoDB`

**Решение**:
```bash
# Проверьте что MongoDB запущена
mongosh mongodb://localhost:27017

# Проверьте логи MongoDB
# Windows
Get-Content "C:\Program Files\MongoDB\Server\6.0\log\mongod.log" -Tail 50

# Linux
sudo tail -f /var/log/mongodb/mongod.log
```

**Проблема**: `Port 5000 is already in use`

**Решение**:
```bash
# Найдите процесс использующий порт
# Windows
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Linux/macOS
lsof -i :5000
kill -9 <PID>

# Или измените порт в настройках
```

### Frontend не подключается к API

**Проблема**: CORS ошибки в консоли браузера

**Решение**: Проверьте настройки CORS в `appsettings.json`:

```json
{
  "AllowedOrigins": [
    "http://localhost:3000"
  ]
}
```

**Проблема**: `Network Error` при запросах

**Решение**:
1. Проверьте что API запущен: `curl http://localhost:5000/health`
2. Проверьте API URL в `client/.env`: `REACT_APP_API_URL=http://localhost:5000`
3. Проверьте консоль браузера на ошибки

### Моделирование работает медленно

**Причины**:
- Слишком большое количество событий
- Слишком частая проверка погрешности
- Недостаточно ресурсов

**Решения**:
1. Увеличьте `generationErrorCheckStep`
2. Уменьшите `maxModelTime` или `maxEvents`
3. Запустите на более мощном сервере

### База данных занимает много места

**Решение**:
```bash
# Удалите старые результаты через API
curl -X DELETE http://localhost:5000/problems/v1/problem/{problemId}

# Или напрямую в MongoDB
mongosh
use SimQDatabase
db.Results.deleteMany({ createdAt: { $lt: ISODate("2024-01-01") } })

# Сжатие БД
db.runCommand({ compact: "Results" })
```

## API

### Как получить список всех задач?

```bash
curl http://localhost:5000/problems/v1/problems
```

### Как получить Swagger документацию?

Откройте в браузере:
```
http://localhost:5000/swagger
```

### Как экспортировать OpenAPI спецификацию?

```bash
curl http://localhost:5000/swagger/v1/swagger.json > openapi.json
```

### Поддерживается ли GraphQL?

В текущей версии нет. Планируется в будущих релизах.

### Есть ли rate limiting?

В текущей версии нет. Рекомендуется добавить на уровне reverse proxy (nginx, API Gateway).

## Производительность

### Сколько событий можно обработать?

Зависит от ресурсов сервера. Типичные показатели:
- **10,000 событий**: < 1 секунды
- **100,000 событий**: 5-10 секунд
- **1,000,000 событий**: 1-2 минуты

### Как ускорить моделирование?

1. **Оптимизируйте параметры**:
   - Увеличьте `generationErrorCheckStep`
   - Используйте меньше агентов

2. **Увеличьте ресурсы**:
   - Больше CPU
   - Больше RAM

3. **Запускайте в фоне**:
   - Используйте Background Jobs
   - Распараллельте задачи

### Можно ли запускать несколько симуляций одновременно?

Да, API поддерживает параллельное выполнение. Ограничение — ресурсы сервера.

### Как масштабировать приложение?

**Горизонтальное масштабирование**:
1. Запустите несколько инстансов API за load balancer
2. Используйте MongoDB Replica Set
3. Добавьте Redis для shared cache

**Вертикальное масштабирование**:
1. Увеличьте ресурсы сервера
2. Оптимизируйте настройки MongoDB
3. Настройте Connection Pooling

См. [DEPLOYMENT.md](docs/DEPLOYMENT.md#масштабирование) для деталей.

## Дополнительные вопросы

### Где найти примеры использования?

- В разделе [API.md](docs/API.md#примеры-использования)
- В папке `examples/` (если создана)
- В тестах проекта `SimQ.Tests/`

### Как внести вклад в проект?

См. [CONTRIBUTING.md](CONTRIBUTING.md)

### Где сообщить о баге?

Создайте issue на GitHub: https://github.com/Ra1mun/SimQ/issues

### Есть ли сообщество или чат?

В данный момент вопросы задаются через GitHub Issues. Возможно создание Discord или Telegram канала в будущем.

---

**Не нашли ответ на свой вопрос?**

Создайте issue на GitHub с меткой "question": https://github.com/Ra1mun/SimQ/issues/new
