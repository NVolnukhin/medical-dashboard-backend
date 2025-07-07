using NotificationService.Enums;

namespace NotificationService.Data.Models;

public class DeadLetterMessage
{
    public Guid Id { get; set; }
    public string MessageBrokerTopic { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
} 