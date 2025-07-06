using Polly;
using Polly.Retry;
using Shared.Extensions.Logging;

namespace DashboardAPI.Services.Kafka.Retry;

public class KafkaRetryService : IKafkaRetryService
{
    private readonly ILogger<KafkaRetryService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public KafkaRetryService(ILogger<KafkaRetryService> logger)
    {
        _logger = logger;
        
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // экспоненциальная задержка
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning($"Попытка {retryCount} для операции {context.OperationKey} не удалась. Ошибка: {exception.Message}. Повтор через {timeSpan.TotalSeconds} секунд.");
                });
    }

    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName)
    {
        var context = new Context(operationName);
        
        return await _retryPolicy.ExecuteAsync(async (ctx) =>
        {
            _logger.LogInfo($"Выполнение операции: {operationName}");
            var result = await operation();
            _logger.LogSuccess($"Операция {operationName} выполнена успешно");
            return result;
        }, context);
    }

    public async Task ExecuteWithRetryAsync(Func<Task> operation, string operationName)
    {
        var context = new Context(operationName);
        
        await _retryPolicy.ExecuteAsync(async (ctx) =>
        {
            _logger.LogInfo($"Выполнение операции: {operationName}");
            await operation();
            _logger.LogSuccess($"Операция {operationName} выполнена успешно");
        }, context);
    }
} 