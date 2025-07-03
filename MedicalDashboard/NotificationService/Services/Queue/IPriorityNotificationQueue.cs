using NotificationService.Data.Models;

namespace NotificationService.Services.Queue;

public interface IPriorityNotificationQueue
{
    Task EnqueueAsync(NotificationRequest notification);
    Task<(bool success, NotificationRequest? notification)> TryDequeueAsync();
    Task<int> GetCountAsync();
} 