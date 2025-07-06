namespace NotificationService.Services.Retry;

public interface IRetryService
{
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, string operationName, CancellationToken cancellationToken = default);
}
