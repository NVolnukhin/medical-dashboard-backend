using Shared.Extensions.Logging;

namespace DataAnalysisService.Services.Kafka.Consumer;

public class KafkaConsumerHostedService : BackgroundService
{
    private readonly IKafkaConsumerService _kafkaConsumerService;
    private readonly ILogger<KafkaConsumerHostedService> _logger;

    public KafkaConsumerHostedService(
        IKafkaConsumerService kafkaConsumerService,
        ILogger<KafkaConsumerHostedService> logger)
    {
        _kafkaConsumerService = kafkaConsumerService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInfo("Запуск Kafka Consumer Hosted Service");
        
        try
        {
            await _kafkaConsumerService.StartConsumingAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка в Kafka Consumer Hosted Service", ex);
        }
        finally
        {
            await _kafkaConsumerService.StopConsumingAsync();
            _logger.LogInfo("Остановлен Kafka Consumer Hosted Service");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInfo("Остановка Kafka Consumer Hosted Service");
        await _kafkaConsumerService.StopConsumingAsync();
        await base.StopAsync(cancellationToken);
    }
} 