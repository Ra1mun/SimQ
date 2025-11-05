# SimQ Шпаргалка

Быстрый справочник по основным командам и операциям SimQ.

## 🚀 Быстрый старт

```bash
# 1. Клонирование
git clone https://github.com/Ra1mun/SimQ.git
cd SimQ

# 2. Запуск MongoDB
docker run -d -p 27017:27017 --name simq-mongo mongo

# 3. Backend
cd SimQ.WebApi
dotnet restore
dotnet run

# 4. Frontend (в новом терминале)
cd client
npm install
npm start
```

## 🔧 Команды разработки

### Backend (.NET)

```bash
# Сборка
dotnet build

# Запуск
dotnet run --project SimQ.WebApi

# Тесты
dotnet test

# Очистка
dotnet clean

# Восстановление пакетов
dotnet restore

# Публикация
dotnet publish -c Release -o ./publish
```

### Frontend (React)

```bash
# Установка зависимостей
npm install

# Запуск dev сервера
npm start

# Сборка для продакшена
npm run build

# Проверка типов
npx tsc --noEmit
```

### MongoDB

```bash
# Подключение
mongosh mongodb://localhost:27017

# Использование БД
use SimQDatabase

# Просмотр коллекций
show collections

# Поиск
db.Problems.find().pretty()

# Создание индекса
db.Problems.createIndex({ "name": 1 })

# Backup
mongodump --db=SimQDatabase --out=/backup/

# Restore
mongorestore --db=SimQDatabase /backup/SimQDatabase/
```

## 📡 API Endpoints

### Problems

```bash
# Получить все задачи
GET /problems/v1/problems

# Получить задачу
GET /problems/v1/problem/{id}

# Создать задачу
POST /problems/v1/problem
Content-Type: application/json
{
  "name": "Task name",
  "agents": [...],
  "links": [...]
}

# Удалить задачу
DELETE /problems/v1/problem/{id}

# Получить результаты
GET /problems/v1/problem/{id}/results

# Получить результат
GET /problems/v1/problem/{id}/result/{resultId}
```

### Tasks

```bash
# Получить все процессы
GET /tasks/v1/tasks

# Запустить задачу
POST /tasks/v1/task/run
Content-Type: application/json
{
  "problemId": "...",
  "parameters": {
    "maxModelTime": 100000.0,
    "seed": 12345
  }
}

# Статус процесса
GET /tasks/v1/task/{taskId}

# Отменить процесс
POST /tasks/v1/task/{taskId}/cancel
```

## 🔨 Примеры curl

### Создать задачу M/M/1

```bash
curl -X POST http://localhost:5000/problems/v1/problem \
  -H "Content-Type: application/json" \
  -d '{
    "name": "M/M/1 System",
    "agents": [
      {
        "id": "source",
        "type": "SourceAgent",
        "eventTag": "arrival",
        "parameters": {"intensity": 0.8, "distributionType": "Exponential"}
      },
      {
        "id": "server",
        "type": "ServiceAgent",
        "eventTag": "service",
        "parameters": {"serviceTime": 1.0, "channelCount": 1, "queueCapacity": 999999}
      }
    ],
    "links": [{"fromAgent": "source", "toAgent": "server"}]
  }'
```

### Запустить моделирование

```bash
curl -X POST http://localhost:5000/tasks/v1/task/run \
  -H "Content-Type: application/json" \
  -d '{
    "problemId": "YOUR_PROBLEM_ID",
    "parameters": {"maxModelTime": 100000.0}
  }'
```

### Получить результаты

```bash
curl http://localhost:5000/problems/v1/problem/YOUR_PROBLEM_ID/results
```

## 🎨 Типы агентов

### SourceAgent (Источник)

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

### ServiceAgent (Канал)

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

### QueueAgent (Очередь)

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

### SinkAgent (Приемник)

```json
{
  "id": "sink1",
  "type": "SinkAgent",
  "eventTag": "departure",
  "parameters": {}
}
```

## 🔗 Связи между агентами

```json
{
  "links": [
    {"fromAgent": "source1", "toAgent": "service1"},
    {"fromAgent": "service1", "toAgent": "queue1"},
    {"fromAgent": "queue1", "toAgent": "sink1"}
  ]
}
```

## ⚙️ Настройки погрешности

```json
{
  "generationErrorSettings": {
    "generationErrorCheckStep": 10000,
    "generationErrorCheckStepModifier": 3,
    "minGenerationError": 0.00001
  }
}
```

## 🐳 Docker

### Docker Compose

```yaml
# docker-compose.yml
version: '3.8'
services:
  mongodb:
    image: mongo:latest
    ports:
      - "27017:27017"
  backend:
    build: ./SimQ.WebApi
    ports:
      - "5000:80"
    environment:
      - DatabaseSettings__ConnectionString=mongodb://mongodb:27017
  frontend:
    build: ./client
    ports:
      - "3000:80"
```

```bash
# Запуск
docker-compose up -d

# Остановка
docker-compose down

# Логи
docker-compose logs -f
```

## 🔍 Отладка

### Backend логи

```bash
# Изменить уровень логирования
# appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "SimQ": "Trace"
    }
  }
}
```

### Проверка здоровья

```bash
# Health check
curl http://localhost:5000/health

# Swagger UI
open http://localhost:5000/swagger
```

## 🔐 Переменные окружения

```bash
# .NET
export ASPNETCORE_ENVIRONMENT=Production
export DatabaseSettings__ConnectionString="mongodb://host:27017"
export DatabaseSettings__DatabaseName="SimQDatabase"

# React
# client/.env
REACT_APP_API_URL=http://localhost:5000
```

## 🧪 Тестирование

```bash
# Unit тесты
dotnet test

# Конкретный проект
dotnet test SimQ.Tests/SimQ.Tests.csproj

# С покрытием
dotnet test --collect:"XPlat Code Coverage"

# Verbose
dotnet test --logger "console;verbosity=detailed"
```

## 📊 Мониторинг

### Проверка портов

```bash
# Windows
netstat -ano | findstr :5000

# Linux/macOS
lsof -i :5000
```

### Проверка процессов

```bash
# Windows
tasklist | findstr dotnet

# Linux
ps aux | grep dotnet
```

## 🚨 Устранение неполадок

### MongoDB не запускается

```bash
# Проверка статуса
mongosh mongodb://localhost:27017

# Логи (Linux)
sudo tail -f /var/log/mongodb/mongod.log

# Перезапуск
sudo systemctl restart mongod
```

### Порт занят

```bash
# Windows
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Linux/macOS
lsof -i :5000
kill -9 <PID>
```

### CORS ошибки

```json
// appsettings.json
{
  "AllowedOrigins": [
    "http://localhost:3000"
  ]
}
```

## 📝 Git команды

```bash
# Создать ветку
git checkout -b feature/my-feature

# Коммит
git add .
git commit -m "feat: add new feature"

# Push
git push origin feature/my-feature

# Синхронизация
git fetch upstream
git rebase upstream/main
```

## 🔢 Типы коммитов (Conventional Commits)

```
feat:     новая функция
fix:      исправление бага
docs:     документация
style:    форматирование
refactor: рефакторинг
test:     тесты
chore:    рутинные задачи
perf:     производительность
```

## 📱 Полезные URL

```
http://localhost:3000          - Frontend
http://localhost:5000          - Backend API
http://localhost:5000/swagger  - Swagger UI
http://localhost:5000/health   - Health Check
mongodb://localhost:27017      - MongoDB
```

## 🆘 Получить помощь

```bash
# Создать issue
open https://github.com/Ra1mun/SimQ/issues/new

# Документация
open https://github.com/Ra1mun/SimQ/tree/main/docs

# FAQ
open https://github.com/Ra1mun/SimQ/blob/main/docs/FAQ.md
```

---

💡 **Совет**: Сохраните эту шпаргалку в закладки браузера для быстрого доступа!

📖 **Полная документация**: [docs/README.md](README.md)
