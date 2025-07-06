using System.Text.Json;
using Confluent.Kafka;
using DataCollectorService.Models;
using Microsoft.Extensions.Options;
using Shared;

namespace DataCollectorService.Kafka
{
    public class KafkaService : IKafkaService, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly KafkaConfig _config;
        private readonly ILogger<KafkaService> _logger;

        public KafkaService(
            IOptions<KafkaConfig> config,
            ILogger<KafkaService> logger)
        {
            _config = config.Value;
            _logger = logger;

            if (_config == null)
            {
                _logger.LogCritical("Kafka configuration is null");
                throw new ArgumentNullException(nameof(config));
            }

            _logger.LogInformation("Kafka configuration: " +
                $"BootstrapServers={_config.BootstrapServers}, " +
                $"RawInformationTopic={_config.RawInformationTopic}, " +
                $"MetricsTopic={_config.MetricsTopic}, " +
                $"ClientId={_config.ClientId}");

            if (string.IsNullOrWhiteSpace(_config.BootstrapServers))
            {
                _logger.LogError("BootstrapServers is not configured");
                throw new ArgumentException("BootstrapServers must be configured");
            }
            var acksValue = string.IsNullOrEmpty(_config.Acks)
            ? "All"
            : _config.Acks;

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _config.BootstrapServers,
                ClientId = _config.ClientId,
                Acks = (Acks)Enum.Parse(typeof(Acks), acksValue),
                MessageTimeoutMs = _config.MessageTimeoutMs
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        }

        public async Task SendToKafka(Patient patient, string metricName, double value)
        {
            try
            {
                var message = new MetricDto
                {
                    PatientId = patient.Id,
                    Type = metricName,
                    Timestamp = DateTime.UtcNow,
                    Value = value
                };

                await ProduceAsync(
                    key: patient.Id.ToString(),
                    message: JsonSerializer.Serialize(message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка отправки {metricName} в Kafka");
            }
        }

        public async Task SendToAllTopics(Patient patient, string metricName, double value)
        {
            try
            {
                var message = new MetricDto
                {
                    PatientId = patient.Id,
                    Type = metricName, 
                    Timestamp = DateTime.UtcNow,
                    Value = value
                };

                var serializedMessage = JsonSerializer.Serialize(message);
                var key = patient.Id.ToString();

                // Отправляем в оба топика
                var tasks = new List<Task>
                {
                    ProduceToTopic(_config.RawInformationTopic, key, serializedMessage),
                    ProduceToTopic(_config.MetricsTopic, key, serializedMessage)
                };

                await Task.WhenAll(tasks);
                _logger.LogDebug($"Отправлено сообщение {metricName} в оба топика для пациента {patient.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка отправки {metricName} в оба топика Kafka");
            }
        }

        private async Task ProduceToTopic(string topic, string key, string message)
        {
            try
            {
                var result = await _producer.ProduceAsync(
                    topic,
                    new Message<string, string> { Key = key, Value = message });

                _logger.LogDebug($"Delivered message to {result.TopicPartitionOffset} (topic: {topic})");
            }
            catch (ProduceException<string, string> e)
            {
                _logger.LogError($"Delivery failed to topic {topic}: {e.Error.Reason}");
            }
        }

        public async Task ProduceAsync(string key, string message)
        {
            try
            {
                var result = await _producer.ProduceAsync(
                    _config.RawInformationTopic,
                    new Message<string, string> { Key = key, Value = message });

                _logger.LogDebug($"Delivered message to {result.TopicPartitionOffset}");
            }
            catch (ProduceException<string, string> e)
            {
                _logger.LogError($"Delivery failed: {e.Error.Reason}");
            }
        }

        public void Dispose()
        {
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();
        }
    }
}
