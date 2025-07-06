using NotificationService.Data.Models;

namespace NotificationService.Services.Notification;

public interface INotificationService
{
    /// <summary>
    /// Отправляет уведомление напрямую получателю
    /// </summary>
    /// <param name="request">Данные для отправки уведомления</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Результат отправки уведомления</returns>
    Task<NotificationResult> SendNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default);
} 