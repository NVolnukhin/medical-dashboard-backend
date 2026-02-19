namespace NotificationService.Config;

public class QueueSettings
{
    public int ProcessingIntervalMs { get; set; } = 100; // Интервал проверки очереди в миллисекундах
} 