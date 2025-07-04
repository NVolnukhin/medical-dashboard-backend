using System.Text.Json;
using AuthService.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Shared.Extensions.Logging;

namespace AuthService.Kafka;

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaConfig _config;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(
        IOptions<KafkaConfig> settings,
        ILogger<KafkaProducerService> logger)
    {
        _config = settings.Value;
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = _config.BootstrapServers,
            ClientId = _config.ProducerClientId
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }
    
    // Конструктор для юнит-тестов
    public KafkaProducerService(
        IOptions<KafkaConfig> settings,
        ILogger<KafkaProducerService> logger,
        IProducer<string, string> testProducer)
    {
        _config = settings.Value;
        _logger = logger;
        _producer = testProducer;
    }

    public async Task SendNotificationAsync(NotificationMessage message)
    {
        try
        {
            var jsonMessage = JsonSerializer.Serialize(message);
            var deliveryResult = await _producer.ProduceAsync(
                _config.TopicName,
                new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = jsonMessage
                });

            _logger.LogSuccess($"Message delivered to {deliveryResult.Topic} [{deliveryResult.Partition}] at offset {deliveryResult.Offset}");
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Error sending message to Kafka", ex);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}