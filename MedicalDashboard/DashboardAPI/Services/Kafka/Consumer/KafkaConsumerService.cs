using System.Text.Json;
using Confluent.Kafka;
using DashboardAPI.Services.Kafka.Retry;
using DashboardAPI.Services.Metric;
using Shared;
using Shared.Extensions.Logging;

namespace DashboardAPI.Services.Kafka.Consumer;

public class KafkaConsumerService : IKafkaConsumerService, IHostedService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IKafkaRetryService _retryService;
    private readonly string _topic;
    private Task? _consumeTask;
    private CancellationTokenSource? _cancellationTokenSource;

    public KafkaConsumerService(
        IConsumer<string, string> consumer,
        ILogger<KafkaConsumerService> logger,
        IServiceScopeFactory scopeFactory,
        IKafkaRetryService retryService,
        string topic)
    {
        _consumer = consumer;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _retryService = retryService;
        _topic = topic;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _consumer.Subscribe(_topic);
        
        _consumeTask = Task.Run(async () =>
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(_cancellationTokenSource.Token);
                        
                        if (consumeResult?.Message?.Value != null)
                        {
                            await ProcessMessageAsync(consumeResult.Message.Value);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при обработке сообщения из Kafka");
                    }
                }
            }
            finally
            {
                _consumer.Close();
            }
        }, _cancellationTokenSource.Token);

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource?.Cancel();
        
        if (_consumeTask != null)
        {
            await _consumeTask;
        }
    }

    private async Task ProcessMessageAsync(string message)
    {
        await _retryService.ExecuteWithRetryAsync(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                
                var metricMessage = JsonSerializer.Deserialize<MetricDto>(message);
                if (metricMessage != null)
                {
                    var metricService = scope.ServiceProvider.GetRequiredService<IMetricService>();
                    await metricService.ProcessMetricFromKafkaAsync(metricMessage);
                }
                else
                {
                    _logger.LogWarning($"Не удалось десериализовать сообщение метрики из топика {_topic}");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogFailure($"Ошибка десериализации JSON из топика {_topic}", ex);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogFailure($"Ошибка при обработке сообщения из топика {_topic}", ex);
                throw;
            }
        }, $"ProcessMessage_{_topic}");
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken) => StartAsync(cancellationToken);
    Task IHostedService.StopAsync(CancellationToken cancellationToken) => StopAsync(cancellationToken);
}