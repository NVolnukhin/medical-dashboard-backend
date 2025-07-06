using NotificationService.Enums;

namespace NotificationService.Data.Models;

public class NotificationTemplate
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> RequiredFields { get; set; } = new();
} 