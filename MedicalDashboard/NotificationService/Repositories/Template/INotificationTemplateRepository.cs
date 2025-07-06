using NotificationService.Data.Models;
using NotificationService.Enums;

namespace NotificationService.Repositories.Template;

public interface INotificationTemplateRepository
{
    Task<NotificationTemplate?> GetBySubjectAndTypeAsync(string subject, NotificationType type, CancellationToken cancellationToken = default);
} 