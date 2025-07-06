using Microsoft.Extensions.Options;
using NotificationService.Config;
using NotificationService.Services.Notification;
using Shared.Extensions.Logging;

namespace NotificationService.Services.Queue;

public class NotificationQueueProcessor : BackgroundService
{
    private readonly IPriorityNotificationQueue _queue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<NotificationQueueProcessor> _logger;
    private readonly TimeSpan _processingInterval;

    public NotificationQueueProcessor(
        IPriorityNotificationQueue queue,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<NotificationQueueProcessor> logger,
        IOptions<QueueSettings> settings)
    {
        _queue = queue;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _processingInterval = TimeSpan.FromMilliseconds(settings.Value.ProcessingIntervalMs);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInfo("NotificationQueueProcessor запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (success, notification) = await _queue.TryDequeueAsync();
                
                if (success && notification != null)
                {
                    _logger.LogInfo($"Обработка сообщения из очереди для получателя {notification.Recipient} с приоритетом {notification.Priority}");
                    
                    using var scope = _serviceScopeFactory.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    
                    var result = await notificationService.SendNotificationAsync(notification, stoppingToken);
                    
                    if (!result.Success)
                    {
                        _logger.LogFailure($"Ошибка отправки уведомления после 3 попыток: {result.ErrorMessage}");
                    }
                    else
                    {
                        _logger.LogSuccess($"Уведомление успешно отправлено получателю {notification.Recipient}");
                    }
                }
                else
                {
                    await Task.Delay(_processingInterval, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogFailure("Ошибка при обработке сообщения из очереди", ex);
                await Task.Delay(_processingInterval, stoppingToken);
            }
        }

        _logger.LogInfo("NotificationQueueProcessor остановлен");
    }
} 