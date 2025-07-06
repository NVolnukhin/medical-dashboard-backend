using System.Text.Json;
using Confluent.Kafka;
using DataAnalysisService.Config;
using Microsoft.Extensions.Options;
using Shared.Extensions.Logging;

namespace DataAnalysisService.Services.Kafka.Producer;

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IOptions<KafkaSettings> kafkaSettings, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = kafkaSettings.Value.BootstrapServers,
            ClientId = "data-analysis-producer"
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync<T>(string topic, T message)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };
            
            var jsonMessage = JsonSerializer.Serialize(message, jsonOptions);
            var kafkaMessage = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = jsonMessage
            };

            var result = await _producer.ProduceAsync(topic, kafkaMessage);
            _logger.LogSuccess($"Сообщение отправлено в топик {result.Topic}, партиция {result.Partition}, смещение {result.Offset}");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка отправки сообщения в топик {topic}", ex);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
} 