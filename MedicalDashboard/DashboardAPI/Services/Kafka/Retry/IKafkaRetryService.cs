namespace DashboardAPI.Services.Kafka.Retry;

public interface IKafkaRetryService
{
    Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, string operationName);
    Task ExecuteWithRetryAsync(Func<Task> operation, string operationName);
} 