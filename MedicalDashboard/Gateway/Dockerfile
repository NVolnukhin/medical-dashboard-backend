# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файлы проектов и восстанавливаем зависимости
COPY ["Gateway/Gateway.csproj", "Gateway/"]
COPY ["Middleware/Middleware.csproj", "Middleware/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Gateway/Gateway.csproj"

# Копируем всё и публикуем
COPY /Gateway/. Gateway/
COPY /Middleware/. Middleware/
COPY /Shared/. Shared/
WORKDIR "/src/Gateway"
RUN dotnet publish "Gateway.csproj" -c Release -o /app/publish

# Этап запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Устанавливаем curl (только в alpine-образе)
RUN apk add --no-cache curl

# Копируем результат публикации
COPY --from=build /app/publish .

# Настройка переменных окружения
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

EXPOSE 80
ENTRYPOINT ["dotnet", "Gateway.dll"]