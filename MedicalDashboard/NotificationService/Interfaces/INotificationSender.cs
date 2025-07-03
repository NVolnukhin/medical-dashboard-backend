using NotificationService.Enums;

namespace NotificationService.Interfaces;

public interface INotificationSender
{
    NotificationType Type { get; }
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
} 