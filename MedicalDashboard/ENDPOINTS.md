# Инфа по всем эндпоинтам

## AuthService 

### Основные эндпоинты
- `POST /identity/login` - Вход в систему
- `POST /identity/refresh-token` - Обновление refresh токена
- `POST /identity/revoke-token` - Отзывает refresh токен (logout)
- `POST /identity/register` - Регистрация пользователя
- `GET /identity/validate` - Валидация JWT токена
- `GET /identity/get-roles` - Получение списка всех ролей
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
- `GET /patient-alerts` - Список алертов (с фильтрацией и пагинацией)
- `GET /patient-alerts/{id}` - Алерт по ID
- `POST /patient-alerts/{id}/ack` - Подтвердить алерт
- `DELETE /patient-alerts/{id}` - Удалить алерт

### Devices
- `GET /devices` — Список устройств (фильтрация по палате, inUsing)
- `GET /devices/{id}` — Информация об устройстве
- `GET /devices/on-patient/{id}` — Краткая информация об устройствах, подключённых к пациенту
- `GET /devices/counting-metrics/{patientId}` — Уникальные метрики по устройствам пациента
- `POST /devices` — Создать устройство
- `PUT /devices/{id}` — Обновить устройство
- `DELETE /devices/{id}` — Удалить устройство
- `POST /devices/attach` — Привязать устройство к пациенту
- `POST /devices/{id}/detach` — Отвязать устройство от пациента

## NotificationService

### Уведомления
- `POST /notifications/notify` - Отправить уведомление
- `GET /notifications/types` - Получить типы уведомлений
- `GET /notifications/priorities` - Получить приоритеты уведомлений

### Dead Letter
- `GET /dead-letters` - Список всех сообщений Dead Letter
- `GET /dead-letters/unprocessed` - Список необработанных сообщений Dead Letter
- `POST /dead-letters/{id}/process` - Отметить сообщение как обработанное

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
// Для получения метрик конкретного пациента в реальном времени
const string hubUrl = "http://localhost:7168/hubs/metrics";
var patientId = Guid.Parse("dd944c9e-1a0a-4b09-9c3a-7c5dc0791001");
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();
```

```csharp
// Для получения алертов в реальном времени
const string hubUrl = "http://localhost:7168/alerts";
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();
```

## Ocelot Gateway Конфигурации

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

## RBAC

| Endpoint                                                | doctor| nurse| admin (audit) | 
|---------------------------------------------------------|-----------|----------|---------------------| 
| POST /identity/login                                    | ✅| ✅| ✅|
| POST /identity/refresh-token                            | ✅| ✅| ✅|
| POST /identity/revoke-token                             | ✅| ✅| ✅|
| POST /identity/register                                 | ❌| ❌| ✅|
| GET /identity/get-roles                                 | ❌| ❌| ✅|
| PUT /identity/update-password                           | ✅| ✅| ✅|
| -------------------------------------------------------| --------- | -------- | ------------------| 
| POST /password-recovery/request                         | ✅| ✅| ✅|
| POST /password-recovery/confirm                         | ✅| ✅| ✅|
| -------------------------------------------------------| --------- | -------- | ------------------| 
| GET /patients                                           | ✅ | ✅| ✅ | 
| GET /patients/{id}                                      | ✅ | ✅| ✅ | 
| POST /patients                                          | ✅ | ❌| ❌ | 
| PUT /patients/{id}                                      | ✅ | ❌| ❌ | 
| DELETE /patients/{id}                                   | ✅ | ❌| ❌ | 
| -------------------------------------------------------| --------- | -------- | ------------------| 
| GET /metrics/{patientId}                                | ✅ | ✅| ✅ | 
| GET /metrics/latest/{patientId}                         | ✅ | ✅| ✅ |
| POST /metrics                                           | ✅ | ❌| ❌ |
| -------------------------------------------------------| --------- | --------| ------------------| 
| GET /patient-alerts                                     | ✅ | ✅| ✅ |
| GET /patient-alerts/{id}                                | ✅ | ✅| ✅ |
| POST /patient-alerts/{id}/ack                           | ✅ | ✅| ✅ |
| DELETE /patient-alerts/{id}                             | ❌ | ❌| ✅ | 
| -------------------------------------------------------| ---------| --------| ------------------|
| GET /devices                                            | ✅ | ✅| ✅ |
| GET /devices/{id}                                       | ✅ | ✅| ✅ |
| GET /devices/on-patient/{id}                            | ✅ | ✅| ✅ |
| GET /devices/counting-metrics/{patientId}               | ✅ | ✅| ✅ |
| POST /devices                                           | ❌ | ❌| ✅ |
| PUT /devices/{id}                                       | ❌ | ❌| ✅ |
| DELETE /devices/{id}                                    | ❌ | ❌| ✅ |
| POST /devices/attach                                    | ✅ | ❌| ❌ |
| POST /devices/{id}/detach                               | ✅ | ❌| ❌ |
| ------------------------------------------------------- | ---------| --------| ------------------|
| Все SignalR hubs                                        | ✅ | ✅| ✅ |
| -------------------------------------------------------| ---------| --------| ------------------|
| POST /notifications/notify                              | ❌| ❌| ✅|
| GET /notifications/types                                | ❌| ❌| ✅| 
| GET /notifications/priorities                           | ❌| ❌| ✅| 
| GET /dead-letters                                       | ❌| ❌| ✅| 
| GET /dead-letters/unprocessed                           | ❌| ❌| ✅|
| POST /dead-letters/{id}/process                         | ❌| ❌| ✅|

