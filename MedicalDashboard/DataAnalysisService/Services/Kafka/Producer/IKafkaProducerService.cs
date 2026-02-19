namespace DataAnalysisService.Services.Kafka.Producer;

public interface IKafkaProducerService
{
    Task ProduceAsync<T>(string topic, T message);
} 