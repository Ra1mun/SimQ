# Руководство по развертыванию SimQ

Это руководство описывает процесс развертывания приложения SimQ в различных окружениях.

## Оглавление

- [Развертывание для разработки](#развертывание-для-разработки)
- [Развертывание в продакшн](#развертывание-в-продакшн)
- [Docker развертывание](#docker-развертывание)
- [Настройка MongoDB](#настройка-mongodb)
- [Настройка веб-сервера](#настройка-веб-сервера)
- [Мониторинг и логирование](#мониторинг-и-логирование)

## Развертывание для разработки

### Предварительные требования

1. **.NET SDK 8.0**
2. **Node.js 16+** и **npm**
3. **MongoDB 4.0+**

### Быстрый старт

#### 1. Клонирование репозитория

```bash
git clone https://github.com/Ra1mun/SimQ.git
cd SimQ
```

#### 2. Запуск MongoDB

**Windows (через службу)**:
```powershell
net start MongoDB
```

**Linux/macOS**:
```bash
sudo systemctl start mongod
# или
brew services start mongodb-community
```

**Docker**:
```bash
docker run -d -p 27017:27017 --name simq-mongo mongo:latest
```

#### 3. Настройка конфигурации

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

#### 4. Запуск Backend

```bash
cd SimQ.WebApi
dotnet restore
dotnet run
```

Приложение будет доступно на:
- `http://localhost:5000`
- `https://localhost:5001`

#### 5. Запуск Frontend

В новом терминале:

```bash
cd client
npm install
npm start
```

Frontend будет доступен на `http://localhost:3000`

---

## Развертывание в продакшн

### Подготовка Backend

#### 1. Сборка приложения

```bash
cd SimQ.WebApi
dotnet publish -c Release -o ./publish
```

#### 2. Настройка конфигурации

Создайте `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "DatabaseSettings": {
    "ConnectionString": "mongodb://your-mongo-server:27017",
    "DatabaseName": "SimQDatabase"
  },
  "AllowedOrigins": [
    "https://your-domain.com"
  ],
  "AllowedHosts": "*"
}
```

**Важно**: Не храните пароли в конфигурационных файлах! Используйте переменные окружения или секреты.

#### 3. Настройка переменных окружения

**Linux/macOS**:
```bash
export ASPNETCORE_ENVIRONMENT=Production
export DatabaseSettings__ConnectionString="mongodb://user:password@server:27017"
export DatabaseSettings__DatabaseName="SimQDatabase"
```

**Windows**:
```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"
$env:DatabaseSettings__ConnectionString="mongodb://user:password@server:27017"
$env:DatabaseSettings__DatabaseName="SimQDatabase"
```

#### 4. Запуск приложения

```bash
cd publish
dotnet SimQ.WebApi.dll
```

### Подготовка Frontend

#### 1. Настройка API URL

Создайте `client/.env.production`:

```
REACT_APP_API_URL=https://api.your-domain.com
```

#### 2. Сборка приложения

```bash
cd client
npm run build
```

Статические файлы будут созданы в `client/build/`

#### 3. Развертывание статики

Скопируйте содержимое `client/build/` на ваш веб-сервер (nginx, Apache, IIS).

---

## Docker развертывание

### Создание Dockerfile для Backend

Создайте `SimQ.WebApi/Dockerfile`:

```dockerfile
# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копирование файлов проекта
COPY ["SimQ.WebApi/SimQ.WebApi.csproj", "SimQ.WebApi/"]
COPY ["SimQ.Core/SimQ.Core.csproj", "SimQ.Core/"]
COPY ["SimQ.DAL/SimQ.DAL.csproj", "SimQ.DAL/"]
COPY ["SimQ.Domain/SimQ.Domain.csproj", "SimQ.Domain/"]

# Восстановление зависимостей
RUN dotnet restore "SimQ.WebApi/SimQ.WebApi.csproj"

# Копирование остальных файлов и сборка
COPY . .
WORKDIR "/src/SimQ.WebApi"
RUN dotnet build "SimQ.WebApi.csproj" -c Release -o /app/build

# Публикация
FROM build AS publish
RUN dotnet publish "SimQ.WebApi.csproj" -c Release -o /app/publish

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Открытие портов
EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "SimQ.WebApi.dll"]
```

### Создание Dockerfile для Frontend

Создайте `client/Dockerfile`:

```dockerfile
# Этап сборки
FROM node:18-alpine AS build
WORKDIR /app

# Копирование package.json и установка зависимостей
COPY package*.json ./
RUN npm ci

# Копирование исходников и сборка
COPY . .
RUN npm run build

# Финальный образ с nginx
FROM nginx:alpine
COPY --from=build /app/build /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### Docker Compose

Создайте `docker-compose.yml` в корне проекта:

```yaml
version: '3.8'

services:
  mongodb:
    image: mongo:latest
    container_name: simq-mongo
    restart: always
    ports:
      - "27017:27017"
    environment:
      MONGO_INITDB_ROOT_USERNAME: admin
      MONGO_INITDB_ROOT_PASSWORD: password
    volumes:
      - mongo-data:/data/db
    networks:
      - simq-network

  backend:
    build:
      context: .
      dockerfile: SimQ.WebApi/Dockerfile
    container_name: simq-backend
    restart: always
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DatabaseSettings__ConnectionString=mongodb://admin:password@mongodb:27017
      - DatabaseSettings__DatabaseName=SimQDatabase
      - AllowedOrigins__0=http://localhost:3000
    depends_on:
      - mongodb
    networks:
      - simq-network

  frontend:
    build:
      context: ./client
      dockerfile: Dockerfile
    container_name: simq-frontend
    restart: always
    ports:
      - "3000:80"
    depends_on:
      - backend
    networks:
      - simq-network

volumes:
  mongo-data:

networks:
  simq-network:
    driver: bridge
```

### Запуск с Docker Compose

```bash
# Сборка и запуск
docker-compose up -d

# Просмотр логов
docker-compose logs -f

# Остановка
docker-compose down

# Остановка с удалением volumes
docker-compose down -v
```

---

## Настройка MongoDB

### Создание пользователя БД

```javascript
// Подключение к MongoDB
mongosh

// Переключение на admin DB
use admin

// Создание администратора
db.createUser({
  user: "admin",
  pwd: "securePassword123",
  roles: ["root"]
})

// Создание пользователя для SimQ
use SimQDatabase
db.createUser({
  user: "simq_user",
  pwd: "simqPassword123",
  roles: [
    { role: "readWrite", db: "SimQDatabase" }
  ]
})
```

### Настройка индексов

```javascript
use SimQDatabase

// Индексы для коллекции Problems
db.Problems.createIndex({ "Name": 1 })
db.Problems.createIndex({ "CreatedAt": -1 })

// Индексы для коллекции Results
db.Results.createIndex({ "ProblemId": 1 })
db.Results.createIndex({ "CreatedAt": -1 })
db.Results.createIndex({ "ProblemId": 1, "CreatedAt": -1 })
```

### Backup и Restore

**Создание бэкапа**:
```bash
mongodump --uri="mongodb://admin:password@localhost:27017" --db=SimQDatabase --out=/backup/
```

**Восстановление**:
```bash
mongorestore --uri="mongodb://admin:password@localhost:27017" --db=SimQDatabase /backup/SimQDatabase/
```

---

## Настройка веб-сервера

### Nginx

Создайте `/etc/nginx/sites-available/simq`:

```nginx
# Frontend
server {
    listen 80;
    server_name your-domain.com;
    root /var/www/simq/frontend;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    # Кэширование статики
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }
}

# Backend API
server {
    listen 80;
    server_name api.your-domain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Активация конфигурации:
```bash
sudo ln -s /etc/nginx/sites-available/simq /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### SSL с Let's Encrypt

```bash
sudo apt-get install certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com -d api.your-domain.com
```

### IIS (Windows)

1. Установите ASP.NET Core Hosting Bundle
2. Создайте новый сайт в IIS Manager
3. Настройте Application Pool с "No Managed Code"
4. Укажите путь к папке publish
5. Настройте HTTPS с SSL сертификатом

---

## Systemd Service (Linux)

Создайте `/etc/systemd/system/simq.service`:

```ini
[Unit]
Description=SimQ Web API
After=network.target

[Service]
Type=notify
WorkingDirectory=/var/www/simq/backend
ExecStart=/usr/bin/dotnet /var/www/simq/backend/SimQ.WebApi.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=simq-api
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

Управление сервисом:
```bash
sudo systemctl daemon-reload
sudo systemctl enable simq
sudo systemctl start simq
sudo systemctl status simq
```

---

## Мониторинг и логирование

### Логирование в файл

Добавьте в `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "File": {
      "Path": "/var/log/simq/app.log",
      "Append": true,
      "MinLevel": "Information"
    }
  }
}
```

### Health Checks

API предоставляет health check endpoint:

```
GET /health
```

Настройка мониторинга:
```bash
# Проверка доступности
curl http://localhost:5000/health

# Автоматическая проверка (cron)
*/5 * * * * curl -f http://localhost:5000/health || systemctl restart simq
```

### Мониторинг с Prometheus

Добавьте метрики в приложение (опционально):

```bash
dotnet add package prometheus-net.AspNetCore
```

### Централизованное логирование

Настройка с Serilog для отправки логов в Elasticsearch/Seq:

```csharp
// В Program.cs
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console()
        .WriteTo.File("logs/simq-.log", rollingInterval: RollingInterval.Day)
        .WriteTo.Seq("http://seq-server:5341");
});
```

---

## Безопасность

### Рекомендации

1. **HTTPS обязателен** в продакшене
2. **Используйте strong passwords** для MongoDB
3. **Настройте firewall** для ограничения доступа к портам
4. **Регулярно обновляйте** зависимости
5. **Используйте secrets management** (Azure Key Vault, HashiCorp Vault)
6. **Настройте rate limiting** для API
7. **Включите CORS** только для доверенных доменов

### Настройка Firewall (UFW)

```bash
sudo ufw allow 22    # SSH
sudo ufw allow 80    # HTTP
sudo ufw allow 443   # HTTPS
sudo ufw deny 27017  # MongoDB (только локальный доступ)
sudo ufw enable
```

---

## Обновление приложения

### Zero-downtime deployment

1. **Сборка новой версии**
2. **Деплой на staging** и тестирование
3. **Blue-Green deployment** или использование load balancer
4. **Мониторинг** после деплоя

### Пример скрипта обновления

```bash
#!/bin/bash
# deploy.sh

# Остановка сервиса
sudo systemctl stop simq

# Бэкап текущей версии
cp -r /var/www/simq/backend /var/www/simq/backend.backup

# Копирование новой версии
cp -r ./publish/* /var/www/simq/backend/

# Запуск сервиса
sudo systemctl start simq

# Проверка
sleep 5
curl -f http://localhost:5000/health || {
    echo "Health check failed, rolling back"
    sudo systemctl stop simq
    rm -rf /var/www/simq/backend
    mv /var/www/simq/backend.backup /var/www/simq/backend
    sudo systemctl start simq
    exit 1
}

echo "Deployment successful"
```

---

## Масштабирование

### Горизонтальное масштабирование

1. **Load Balancer** (nginx, HAProxy) перед несколькими инстансами API
2. **MongoDB Replica Set** для отказоустойчивости БД
3. **Redis** для shared кэша между инстансами
4. **CDN** для статики frontend

### Вертикальное масштабирование

- Увеличение ресурсов сервера (CPU, RAM)
- Оптимизация настроек MongoDB
- Настройка Connection Pooling

---

## Troubleshooting

### Проблемы с подключением к MongoDB

```bash
# Проверка доступности MongoDB
mongosh mongodb://localhost:27017

# Проверка логов MongoDB
sudo tail -f /var/log/mongodb/mongod.log
```

### Проблемы с ASP.NET Core

```bash
# Проверка логов приложения
sudo journalctl -u simq -f

# Проверка переменных окружения
sudo systemctl show simq --property=Environment
```

### Проблемы с Frontend

- Проверьте console в браузере на ошибки
- Убедитесь что CORS настроен правильно
- Проверьте что API URL корректный в `.env`

---

## Контакты и поддержка

Для вопросов по развертыванию создавайте issue в репозитории проекта.
