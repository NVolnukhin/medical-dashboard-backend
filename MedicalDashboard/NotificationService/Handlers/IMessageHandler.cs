namespace NotificationService.Handlers;

public interface IMessageHandler<T>
{
    string Topic { get; }
    Task HandleAsync(T message, CancellationToken cancellationToken = default);
} 