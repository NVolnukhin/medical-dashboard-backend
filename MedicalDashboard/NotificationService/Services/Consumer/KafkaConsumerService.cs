using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using NotificationService.Data.Models;
using NotificationService.Extensions.Logging;
using NotificationService.Handlers;

namespace NotificationService.Services.Consumer;

public class KafkaConsumerService<T> : BackgroundService, IMessageBroker
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaConsumerService<T>> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentDictionary<string, Task> _processingTasks;
    private readonly CancellationTokenSource _internalCts;
    private const int MaxConcurrentOperations = 5;
    private const int KafkaConnectionTimeoutSeconds = 5;
    private bool _isInitialized;

    public KafkaConsumerService(
        IConsumer<string, string> consumer,
        ILogger<KafkaConsumerService<T>> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        //Проверка сервисов
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(consumer);
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);

        _logger = logger;
        _consumer = consumer;
        _serviceScopeFactory = serviceScopeFactory;
        _semaphore = new SemaphoreSlim(MaxConcurrentOperations);
        _processingTasks = new ConcurrentDictionary<string, Task>();
        _internalCts = new CancellationTokenSource();
        _isInitialized = false;

        _logger.LogSuccess($"KafkaConsumerService<{typeof(T).Name}> успешно инициализирован");
    }
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInfo($"KafkaConsumerService<{typeof(T).Name}>.StartAsync started");
            
            // Запускаем инициализацию Kafka в отдельной задаче
            _ = Task.Run(async () =>
            {
                try
                {
                    await InitializeKafkaAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogFailure($"Ошибка инициализации Kafka: {ex.Message}", ex);
                }
            }, cancellationToken);

            await base.StartAsync(cancellationToken);
            _logger.LogInfo($"KafkaConsumerService<{typeof(T).Name}>.StartAsync completed");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка в KafkaConsumerService<{typeof(T).Name}>.StartAsync", ex);
            throw;
        }
    }

    private async Task InitializeKafkaAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var messageHandler = scope.ServiceProvider.GetRequiredService<IMessageHandler<T>>();
            var topic = messageHandler.Topic;

            _logger.LogInfo($"Подписываемся на топик: {topic}");
            
            // Добавляем таймаут для подписки
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(KafkaConnectionTimeoutSeconds));

            try
            {
                _consumer.Subscribe(topic);
                _isInitialized = true;
                _logger.LogSuccess($"Успешно подписаны на: {topic}");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning($"Таймаут при подписке на топик: {topic}");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка инициализации Kafka: {ex.Message}", ex);
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInfo($"KafkaConsumerService<{typeof(T).Name}>.StopAsync started");
            await base.StopAsync(cancellationToken);
            _logger.LogInfo($"KafkaConsumerService<{typeof(T).Name}>.StopAsync completed");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка в KafkaConsumerService<{typeof(T).Name}>.StopAsync", ex);
            throw;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInfo($"KafkaConsumerService<{typeof(T).Name}>.ExecuteAsync started");

        if (_consumer == null)
        {
            _logger.LogFailure("Consumer не инициализирован");
            throw new InvalidOperationException("Consumer не инициализирован");
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Если Kafka не инициализирована, ждем и пробуем снова
                    if (!_isInitialized)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        continue;
                    }

                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if (consumeResult == null || consumeResult.Message?.Value == null)
                        continue;

                    _logger.LogInfo($"Получено новое сообщение из топика: {consumeResult.Topic}");

                    // Очищаем завершенные задачи
                    var completedTasks = _processingTasks.Where(kvp => kvp.Value.IsCompleted).ToList();
                    foreach (var task in completedTasks)
                    {
                        _processingTasks.TryRemove(task.Key, out _);
                    }

                    // Используем таймаут для ожидания семафора
                    if (!await _semaphore.WaitAsync(TimeSpan.FromSeconds(5), stoppingToken))
                    {
                        _logger.LogWarning("Таймаут при ожидании семафора");
                        continue;
                    }

                    var messageId = Guid.NewGuid().ToString();
                    var processingTask = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessMessageAsync(consumeResult, stoppingToken);
                            _consumer.Commit(consumeResult);
                            _logger.LogSuccess($"Сообщение {messageId} успешно обработано");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogFailure($"Ошибка обработки сообщения {messageId}", ex);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }, stoppingToken);

                    _processingTasks.TryAdd(messageId, processingTask);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogFailure("Error consuming message", ex);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogFailure("Unexpected error in consumption loop", ex);
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }

            // Ждем завершения всех задач с таймаутом
            try
            {
                await Task.WhenAll(_processingTasks.Values)
                    .WaitAsync(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Таймаут при ожидании завершения задач");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInfo($"KafkaConsumerService<{typeof(T).Name}> is stopping");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Fatal error in KafkaConsumerService<{typeof(T).Name}>", ex);
            throw;
        }
        finally
        {
            try
            {
                _consumer.Close();
                _semaphore.Dispose();
                _internalCts.Dispose();
                _logger.LogInfo($"KafkaConsumerService<{typeof(T).Name}> stopped");
            }
            catch (Exception ex)
            {
                _logger.LogFailure($"Error during cleanup", ex);
            }
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInfo($"Содержимое сообщения: {consumeResult.Message.Value}");

            // Проверка структуры сообщения
            if (typeof(T) == typeof(NotificationRequest))
            {
                var jsonDocument = JsonDocument.Parse(consumeResult.Message.Value);
                if (!jsonDocument.RootElement.TryGetProperty("Recipient", out _) ||
                    !jsonDocument.RootElement.TryGetProperty("Subject", out _) ||
                    !jsonDocument.RootElement.TryGetProperty("Body", out _))
                {
                    _logger.LogWarning($"Сообщение не соответствует структуре NotificationRequest: {consumeResult.Message.Value}");
                    return;
                }
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var message = JsonSerializer.Deserialize<T>(consumeResult.Message.Value, options);
            if (message == null)
            {
                _logger.LogWarning($"Ошибка десериализации сообщения: {consumeResult.Message.Value}");
                return;
            }

            _logger.LogInfo($"Сообщение успешно десериализовано в тип {typeof(T).Name}");

            using var scope = _serviceScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<T>>();
            await handler.HandleAsync(message, stoppingToken);
        }
        catch (JsonException ex)
        {
            _logger.LogFailure($"JSON deserialization error for message: {consumeResult.Message.Value}", ex);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка обработки сообщения", ex);
            throw;
        }
    }

    public override void Dispose()
    {
        try
        {
            _consumer.Dispose();
            _semaphore.Dispose();
            base.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка в Dispose", ex);
            throw;
        }
    }
} 