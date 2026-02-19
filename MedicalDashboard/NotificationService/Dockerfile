# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем проекты
COPY /NotificationService/ ./NotificationService/
COPY /Shared/ ./Shared/

# Восстановление зависимостей
WORKDIR /src/NotificationService
RUN dotnet restore NotificationService.csproj

# Публикация
RUN dotnet publish NotificationService.csproj -c Release -o /app/publish

# Этап запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Настройки окружения
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:80

EXPOSE 80
ENTRYPOINT ["dotnet", "NotificationService.dll"]