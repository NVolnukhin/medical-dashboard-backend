using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Data.Models;
using NotificationService.Enums;

namespace NotificationService.Repositories.Template;

public class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationTemplateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationTemplate?> GetBySubjectAndTypeAsync(string subject, NotificationType type, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Subject == subject && t.Type == type, cancellationToken);
    }
}