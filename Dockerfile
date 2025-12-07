# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копирование файлов проектов для восстановления зависимостей
COPY ["SimQ.WebApi/SimQ.WebApi.csproj", "SimQ.WebApi/"]
COPY ["SimQ.Core/SimQ.Core.csproj", "SimQ.Core/"]
COPY ["SimQ.DAL/SimQ.DAL.csproj", "SimQ.DAL/"]
COPY ["SimQ.Domain/SimQ.Domain.csproj", "SimQ.Domain/"]
COPY ["SimQ.Simulation/SimQ.Simulation.csproj", "SimQ.Simulation/"]

# Восстановление зависимостей
RUN dotnet restore "SimQ.WebApi/SimQ.WebApi.csproj"

# Копирование всех исходников
COPY . .

# Сборка проекта
WORKDIR "/src/SimQ.WebApi"
RUN dotnet build "SimQ.WebApi.csproj" -c Release -o /app/build

# Публикация
FROM build AS publish
RUN dotnet publish "SimQ.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Создание пользователя без прав root для безопасности
RUN useradd -m -u 1000 simquser && chown -R simquser:simquser /app
USER simquser

# Копирование опубликованного приложения
COPY --from=publish /app/publish .

# Открытие портов
EXPOSE 8080
EXPOSE 8081

# Переменные окружения по умолчанию
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DatabaseSettings__ConnectionString=mongodb://mongodb:27017 \
    DatabaseSettings__DatabaseName=SimQDatabase

ENTRYPOINT ["dotnet", "SimQ.WebApi.dll"]
