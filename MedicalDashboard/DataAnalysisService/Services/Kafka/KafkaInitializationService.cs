using Confluent.Kafka;
using Confluent.Kafka.Admin;
using DataAnalysisService.Config;
using Microsoft.Extensions.Options;
using Shared.Extensions.Logging;

namespace DataAnalysisService.Services.Kafka;

public class KafkaInitializationService : IHostedService
{
    private readonly ILogger<KafkaInitializationService> _logger;
    private readonly IAdminClient _adminClient;
    private readonly KafkaSettings _kafkaSettings;

    public KafkaInitializationService(
        ILogger<KafkaInitializationService> logger,
        IOptions<KafkaSettings> kafkaOptions)
    {
        _logger = logger;
        _kafkaSettings = kafkaOptions.Value;

        var config = new AdminClientConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers
        };

        _adminClient = new AdminClientBuilder(config).Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInfo("Начало инициализации Kafka топиков для DataAnalysisService");

            // Список топиков для создания
            var topics = new[]
            {
                _kafkaSettings.Topics.RawMetrics,  // md-raw-information
                _kafkaSettings.Topics.Alerts       // md-alerts
            };

            foreach (var topic in topics)
            {
                try
                {
                    var topicSpec = new TopicSpecification
                    {
                        Name = topic,
                        ReplicationFactor = 1,
                        NumPartitions = 3
                    };

                    await _adminClient.CreateTopicsAsync(new[] { topicSpec });
                    _logger.LogSuccess($"Топик {topic} успешно создан");
                }
                catch (CreateTopicsException ex) when (ex.Message.Contains("already exists"))
                {
                    _logger.LogInfo($"Топик {topic} уже существует");
                }
                catch (Exception ex)
                {
                    _logger.LogFailure($"Ошибка при создании топика {topic}", ex);
                }
            }

            _logger.LogSuccess("Инициализация Kafka топиков для DataAnalysisService завершена");
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка при инициализации Kafka топиков для DataAnalysisService", ex);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _adminClient.Dispose();
        return Task.CompletedTask;
    }
} 