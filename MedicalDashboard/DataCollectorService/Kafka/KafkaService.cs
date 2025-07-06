using Confluent.Kafka;
using System;
using Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Kafka
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
                $"Topic={_config.Topic}, " +
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
                var message = new
                {
                    PatientId = patient.Id,
                    PatientName = patient.Name,
                    Metric = metricName,
                    Value = value,
                    Timestamp = DateTime.UtcNow
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
        public async Task ProduceAsync(string key, string message)
        {
            try
            {
                var result = await _producer.ProduceAsync(
                    _config.Topic,
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
