using NotificationService.Data.Models;
using NotificationService.Enums;
using Shared.Extensions.Logging;

namespace NotificationService.Services.Queue;

public class PriorityNotificationQueue : IPriorityNotificationQueue
{
    private readonly PriorityQueue<NotificationRequest, int> _queue;
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<PriorityNotificationQueue> _logger;

    public PriorityNotificationQueue(ILogger<PriorityNotificationQueue> logger)
    {
        _queue = new PriorityQueue<NotificationRequest, int>();
        _semaphore = new SemaphoreSlim(1, 1);
        _logger = logger;
    }

    public async Task EnqueueAsync(NotificationRequest notification)
    {
        try
        {
            await _semaphore.WaitAsync();
            try
            {
                var priority = (int)notification.Priority;
                _queue.Enqueue(notification, priority);
                _logger.LogInfo($"Сообщение добавлено в очередь с приоритетом {notification.Priority}. Получатель: {notification.Recipient}");
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при добавлении сообщения в очередь", ex);
            throw;
        }
    }

    public async Task<(bool success, NotificationRequest? notification)> TryDequeueAsync()
    {
        try
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_queue.TryDequeue(out var notification, out var priority))
                {
                    _logger.LogInfo($"Сообщение извлечено из очереди с приоритетом {(NotificationPriority)priority}. Получатель: {notification.Recipient}");
                    return (true, notification);
                }
                return (false, null);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка при извлечении сообщения из очереди", ex);
            throw;
        }
    }

    public async Task<int> GetCountAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            return _queue.Count;
        }
        finally
        {
            _semaphore.Release();
        }
    }
} 