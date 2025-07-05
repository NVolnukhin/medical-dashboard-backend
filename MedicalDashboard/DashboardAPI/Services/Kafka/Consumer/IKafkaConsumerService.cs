namespace DashboardAPI.Services.Kafka.Consumer;

public interface IKafkaConsumerService
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
} 