# Инфа по всем эндпоинтам

## AuthService 

### Основные эндпоинты
- `POST /identity/login` - Вход в систему
- `POST /identity/register` - Регистрация пользователя
- `GET /identity/validate` - Валидация JWT токена
- `PUT /identity/update-password` - Смена пароля (требует авторизацию)

### Восстановление пароля
- `POST /password-recovery/request` - Запрос восстановления пароля
- `POST /password-recovery/confirm` - Подтверждение восстановления

## DashboardAPI 

### Пациенты
- `GET /patients` - Список пациентов (с фильтрацией и пагинацией)
- `GET /patients/{id}` - Пациент по ID
- `POST /patients` - Создать пациента
- `PUT /patients/{id}` - Обновить пациента
- `DELETE /patients/{id}` - Удалить пациента

### Метрики
- `GET /metrics/{patientId}` - Метрики пациента (с фильтрацией по времени и типу)
- `GET /metrics/latest/{patientId}` - Последние метрики пациента
- `POST /metrics` - Создать метрику

### Алерты
- `GET /alerts` - Список алертов (с фильтрацией и пагинацией)
- `GET /alerts/{id}` - Алерт по ID
- `POST /alerts/{id}/ack` - Подтвердить алерт
- `DELETE /alerts/{id}` - Удалить алерт

## NotificationService

### Уведомления
- `POST /notifications/notify` - Отправить уведомление
- `GET /notifications/types` - Получить типы уведомлений
- `GET /notifications/priorities` - Получить приоритеты уведомлений

## SignalR

### WebSocket эндпоинты
- `GET /hubs/metrics` - SignalR Hub для метрик
- `POST /hubs/metrics` - SignalR negotiation
- `GET /hubs/metrics/{catchAll}` - Catch-all WebSocket соединения
- `POST /hubs/metrics/{catchAll}` - Catch-all POST

### Методы Hub
- `SubscribeToPatient(patientId)` - Подписка на метрики пациента
- `UnsubscribeFromPatient(patientId)` - Отписка от метрик пациента

### События Hub
- `ReceiveMetric(metric)` - Получение новой метрики
- `SubscribedToPatient(patientId)` - Подтверждение подписки
- `UnsubscribedFromPatient(patientId)` - Подтверждение отписки

### Подключение через gateway
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:7168/hubs/metrics")
    .build();
```

```csharp
const string hubUrl = "http://localhost:7168/hubs/metrics";
var patientId = Guid.Parse("dd944c9e-1a0a-4b09-9c3a-7c5dc0791001");
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();
```

## 🔧 Ocelot Gateway Конфигурации

### development
- **AuthService**: `localhost:5251`
- **DashboardAPI**: `localhost:5259`
- **NotificationService**: `localhost:5250`

### prod
- **AuthService**: `auth-service:80`
- **DashboardAPI**: `dashboard-api:80`
- **NotificationService**: `notification-service:80`

## Филтры для запросов

### Пациенты
- `name` - Поиск по ФИО
- `ward` - Фильтр по номеру палаты
- `doctorId` - Фильтр по ID врача
- `page` - Номер страницы (def: 1)
- `pageSize` - Размер страницы (def: 20, max: 100)

### Метрики
- `startPeriod` - Начало периода
- `endPeriod` - конец периода
- `type` - Тип метрики

### Алерты
- `patientId` - Фильтр по ID пациента
- `isProcessed` - Фильтр по статусу обработки
- `page` - Номер страницы
- `pageSize` - Размер страницы

## Аутентификация

### Защищенные эндпоинты
Все эндпоинты DashboardAPI требуют JWT Bearer токен в заголовке:
```
Authorization: Bearer <jwt_token>
```

### Публичные эндпоинты
- Все эндпоинты AuthService (кроме `/identity/update-password`)
- Все эндпоинты NotificationService


## Заголовки ответов

Для списков возвращаются заголовки пагинации:
- `X-Total-Count` - Общее количество
- `X-Page` - Текущая страница
- `X-PageSize` - Размер страницы