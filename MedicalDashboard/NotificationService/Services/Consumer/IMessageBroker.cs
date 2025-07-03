namespace NotificationService.Services.Consumer;

public interface IMessageBroker
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
} 