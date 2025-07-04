using Microsoft.Extensions.Options;
using NotificationService.Config;
using Shared.Extensions.Logging;

namespace NotificationService.Services.Retry;

public class RetryService : IRetryService
{
    private readonly ILogger<RetryService> _logger;
    private readonly RetrySettings _settings;

    public RetryService(
        ILogger<RetryService> logger,
        IOptions<RetrySettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> action,
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var retryCount = 0;
        var lastException = default(Exception);
        var startTime = DateTime.UtcNow;

        while (retryCount < _settings.MaxRetryAttempts)
        {
            try
            {
                _logger.LogInfo($"Попытка {retryCount + 1} из {_settings.MaxRetryAttempts} для '{operationName}'");
                
                try
                {
                    var result = await action();
                    var duration = DateTime.UtcNow - startTime;
                    _logger.LogSuccess($"Успешно выполнено '{operationName}' за {retryCount + 1} попытку(и). Затрачено {duration.TotalSeconds:F1} секунд");
                    return result;
                }
                catch (TimeoutException)
                {
                    _logger.LogWarning($"Для попытки {retryCount + 1} из {_settings.MaxRetryAttempts} время ожидания истекло после {_settings.OperationTimeoutSeconds} секунд");
                    retryCount++;
                    continue;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                retryCount++;
                var duration = DateTime.UtcNow - startTime;

                if (retryCount >= _settings.MaxRetryAttempts)
                {
                    _logger.LogFailure($"Операция '{operationName}' неуспешна. {retryCount} попыток и {duration.TotalSeconds:F1} секунд затрачено", ex);
                    throw;
                }

                _logger.LogWarning($"Попытка {retryCount} из {_settings.MaxRetryAttempts} неуспешна. Затрачено {duration.TotalSeconds:F1} секунд. Ошибка: {ex.Message}. Повторяем...");
            }
        }

        throw lastException ?? new Exception($"Операция '{operationName}' неуспешна после {retryCount} попыток");
    }
} 