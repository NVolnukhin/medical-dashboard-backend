namespace NotificationService.Config;

public class RetrySettings
{
    public int MaxRetryAttempts { get; set; } = 3;
    public int OperationTimeoutSeconds { get; set; } = 20;
} 