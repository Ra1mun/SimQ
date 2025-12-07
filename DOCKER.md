# SimQ Docker Deployment

Это руководство описывает как запустить SimQ используя Docker и Docker Compose.

## 🚀 Быстрый старт

### Предварительные требования

- Docker 20.10 или выше
- Docker Compose 2.0 или выше

### Запуск

1. **Клонируйте репозиторий**:
```bash
git clone https://github.com/Ra1mun/SimQ.git
cd SimQ
```

2. **Создайте файл .env**:
```bash
cp .env.example .env
```

Отредактируйте `.env` и установите безопасный пароль для MongoDB:
```env
MONGO_PASSWORD=your_secure_password_here
```

3. **Запустите контейнеры**:
```bash
docker-compose up -d
```

4. **Проверьте статус**:
```bash
docker-compose ps
```

5. **Откройте приложение**:
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

## 📦 Структура контейнеров

```
┌─────────────────┐
│   Frontend      │  :3000
│   (nginx)       │
└────────┬────────┘
         │
┌────────┴────────┐
│   Backend       │  :5000
│   (ASP.NET)     │
└────────┬────────┘
         │
┌────────┴────────┐
│   MongoDB       │  :27017
└─────────────────┘
```

## 🔧 Конфигурация

### Переменные окружения

Создайте файл `.env` на основе `.env.example`:

```env
# MongoDB
MONGO_PASSWORD=your_secure_password_here
MONGO_PORT=27017

# Backend
BACKEND_PORT=5000
ASPNETCORE_ENVIRONMENT=Production
DATABASE_NAME=SimQDatabase
LOG_LEVEL=Information

# Frontend
FRONTEND_PORT=3000
BACKEND_URL=http://localhost:5000
FRONTEND_URL=http://localhost:3000
```

### Изменение портов

Если порты 3000 или 5000 заняты, измените их в `.env`:

```env
BACKEND_PORT=8080
FRONTEND_PORT=8081
```

## 🛠️ Команды Docker Compose

### Запуск

```bash
# Запуск в фоновом режиме
docker-compose up -d

# Запуск с просмотром логов
docker-compose up

# Запуск только backend и MongoDB
docker-compose up -d backend mongodb
```

### Остановка

```bash
# Остановка всех контейнеров
docker-compose down

# Остановка с удалением volumes (БД будет очищена!)
docker-compose down -v

# Остановка только frontend
docker-compose stop frontend
```

### Логи

```bash
# Все логи
docker-compose logs -f

# Логи конкретного сервиса
docker-compose logs -f backend
docker-compose logs -f mongodb

# Последние 100 строк
docker-compose logs --tail=100 backend
```

### Пересборка

```bash
# Пересборка всех образов
docker-compose build

# Пересборка конкретного сервиса
docker-compose build backend

# Пересборка без кэша
docker-compose build --no-cache

# Пересборка и перезапуск
docker-compose up -d --build
```

### Очистка

```bash
# Удалить неиспользуемые образы
docker image prune -a

# Удалить все неиспользуемые ресурсы
docker system prune -a --volumes
```

## 🔍 Отладка

### Проверка health checks

```bash
docker-compose ps
```

Все сервисы должны показывать статус `healthy`.

### Подключение к контейнеру

```bash
# Backend
docker exec -it simq-backend /bin/bash

# Frontend
docker exec -it simq-frontend /bin/sh

# MongoDB
docker exec -it simq-mongodb mongosh -u admin -p your_password
```

### Проверка сети

```bash
# Список сетей
docker network ls

# Информация о сети simq
docker network inspect simq_simq-network
```

## 📊 Мониторинг

### Использование ресурсов

```bash
# Статистика в реальном времени
docker stats

# Конкретный контейнер
docker stats simq-backend
```

### Health checks

```bash
# Backend
curl http://localhost:5000/health

# Frontend
curl http://localhost:3000

# MongoDB
docker exec simq-mongodb mongosh --eval "db.adminCommand('ping')"
```

## 💾 Backup и Restore

### Backup MongoDB

```bash
# Создание backup
docker exec simq-mongodb mongodump \
  --username admin \
  --password your_password \
  --authenticationDatabase admin \
  --db SimQDatabase \
  --out /tmp/backup

# Копирование backup на хост
docker cp simq-mongodb:/tmp/backup ./backup
```

### Restore MongoDB

```bash
# Копирование backup в контейнер
docker cp ./backup simq-mongodb:/tmp/backup

# Восстановление
docker exec simq-mongodb mongorestore \
  --username admin \
  --password your_password \
  --authenticationDatabase admin \
  --db SimQDatabase \
  /tmp/backup/SimQDatabase
```

## 🔒 Безопасность

### Рекомендации для продакшена

1. **Не используйте default пароли**:
   ```env
   MONGO_PASSWORD=$(openssl rand -base64 32)
   ```

2. **Используйте HTTPS**:
   - Настройте reverse proxy (nginx/traefik) с SSL
   - Используйте Let's Encrypt для сертификатов

3. **Ограничьте доступ к портам**:
   ```yaml
   # В docker-compose.yml уберите публикацию порта MongoDB
   mongodb:
     # ports:
     #   - "27017:27017"  # Закомментируйте эту строку
   ```

4. **Используйте secrets**:
   ```yaml
   secrets:
     mongo_password:
       file: ./secrets/mongo_password.txt
   ```

5. **Обновляйте образы**:
   ```bash
   docker-compose pull
   docker-compose up -d
   ```

## 🚀 Production Deployment

### С reverse proxy (nginx)

Создайте `nginx.conf` для reverse proxy:

```nginx
upstream backend {
    server localhost:5000;
}

upstream frontend {
    server localhost:3000;
}

server {
    listen 80;
    server_name yourdomain.com;

    location / {
        proxy_pass http://frontend;
    }

    location /api {
        proxy_pass http://backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### С Docker Swarm

```bash
# Инициализация Swarm
docker swarm init

# Деплой stack
docker stack deploy -c docker-compose.yml simq

# Проверка сервисов
docker service ls

# Масштабирование
docker service scale simq_backend=3
```

## 🐛 Troubleshooting

### Контейнеры не запускаются

```bash
# Проверьте логи
docker-compose logs

# Проверьте конфигурацию
docker-compose config

# Удалите старые контейнеры и volumes
docker-compose down -v
docker-compose up -d
```

### Порты заняты

```bash
# Windows
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Linux/macOS
lsof -i :5000
kill -9 <PID>

# Или измените порты в .env
```

### Проблемы с сетью

```bash
# Пересоздайте сеть
docker-compose down
docker network prune
docker-compose up -d
```

### MongoDB не запускается

```bash
# Проверьте логи
docker-compose logs mongodb

# Удалите volume и пересоздайте
docker-compose down -v
docker volume rm simq_mongodb_data
docker-compose up -d mongodb
```

## 📚 Дополнительные ресурсы

- [Docker документация](https://docs.docker.com/)
- [Docker Compose документация](https://docs.docker.com/compose/)
- [MongoDB Docker Hub](https://hub.docker.com/_/mongo)
- [ASP.NET Core Docker](https://docs.microsoft.com/aspnet/core/host-and-deploy/docker/)

## 🤝 Поддержка

Если у вас возникли проблемы:

1. Проверьте [FAQ](docs/FAQ.md)
2. Изучите [документацию по развертыванию](docs/DEPLOYMENT.md)
3. Создайте [issue на GitHub](https://github.com/Ra1mun/SimQ/issues)

---

**Примечание**: Для продакшена рекомендуется использовать orchestration системы как Kubernetes или Docker Swarm для высокой доступности и масштабируемости.
