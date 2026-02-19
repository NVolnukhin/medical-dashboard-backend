using System.Text.Json;
using Confluent.Kafka;
using DataAnalysisService.Config;
using DataAnalysisService.Services.Analysis;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Extensions.Logging;

namespace DataAnalysisService.Services.Kafka.Consumer;

public class KafkaConsumerService : IKafkaConsumerService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IDataAnalysisService _dataAnalysisService;
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public KafkaConsumerService(
        IOptions<KafkaSettings> kafkaSettings,
        IDataAnalysisService dataAnalysisService,
        ILogger<KafkaConsumerService> logger)
    {
        _kafkaSettings = kafkaSettings.Value;
        _dataAnalysisService = dataAnalysisService;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();

        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaSettings.Value.BootstrapServers,
            GroupId = kafkaSettings.Value.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _consumer.Subscribe(_kafkaSettings.Topics.RawMetrics);
            _logger.LogSuccess($"Начато потребление из топика: {_kafkaSettings.Topics.RawMetrics}");

            while (!cancellationToken.IsCancellationRequested && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));
                    if (consumeResult == null) continue;

                    await ProcessMessageAsync(consumeResult);
                    _consumer.Commit(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogFailure("Ошибка потребления сообщения", ex);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInfo("Операция потребления отменена");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка в цикле потребителя", ex);
        }
        finally
        {
            _consumer.Close();
        }
    }

    public Task StopConsumingAsync()
    {
        _cancellationTokenSource.Cancel();
        _logger.LogInfo("Потребление остановлено");
        return Task.CompletedTask;
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult)
    {
        try
        {
            var metricDto = JsonSerializer.Deserialize<MetricDto>(consumeResult.Message.Value);
            if (metricDto == null)
            {
                _logger.LogWarning("Не удалось десериализовать сообщение: {Message}", consumeResult.Message.Value);
                return;
            }

            _logger.LogInfo($"Получена метрика: PatientId={metricDto.PatientId}, Type={metricDto.Type}, Value={metricDto.Value}");

            await _dataAnalysisService.AnalyzeMetricAsync(metricDto);
        }
        catch (JsonException ex)
        {
            _logger.LogFailure($"Не удалось десериализовать сообщение: {consumeResult.Message.Value}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка обработки сообщения", ex);
        }
    }
} 