namespace DataAnalysisService.Services.Kafka.Consumer;

public interface IKafkaConsumerService
{
    Task StartConsumingAsync(CancellationToken cancellationToken = default);
    Task StopConsumingAsync();
} 