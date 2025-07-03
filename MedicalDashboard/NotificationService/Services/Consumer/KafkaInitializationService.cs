using Confluent.Kafka;
using Confluent.Kafka.Admin;
using NotificationService.Config;
using NotificationService.Extensions.Logging;

namespace NotificationService.Services.Consumer;

public class KafkaInitializationService : IHostedService
{
    private readonly ILogger<KafkaInitializationService> _logger;
    private readonly IAdminClient _adminClient;

    public KafkaInitializationService(
        ILogger<KafkaInitializationService> logger,
        KafkaSettings kafkaSettings)
    {
        _logger = logger;

        var config = new AdminClientConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers
        };

        _adminClient = new AdminClientBuilder(config).Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInfo("Начало инициализации Kafka топиков");

            // Список топиков для создания
            var topics = new[]
            {
                "md-emails",
                "md-alerts"
            };

            foreach (var topic in topics)
            {
                try
                {
                    var topicSpec = new TopicSpecification
                    {
                        Name = topic,
                        ReplicationFactor = 1,
                        NumPartitions = 1 
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

            _logger.LogSuccess("Инициализация Kafka топиков завершена");
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка при инициализации Kafka топиков", ex);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _adminClient.Dispose();
        return Task.CompletedTask;
    }
} 