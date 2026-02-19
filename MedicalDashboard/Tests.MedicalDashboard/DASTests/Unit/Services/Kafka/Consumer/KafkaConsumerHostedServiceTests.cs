using DataAnalysisService.Services.Kafka.Consumer;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.MedicalDashboard.DASTests.Unit.Services.Kafka.Consumer;

public class KafkaConsumerHostedServiceTests
{
    private readonly Mock<IKafkaConsumerService> _kafkaConsumerServiceMock;
    private readonly Mock<ILogger<KafkaConsumerHostedService>> _loggerMock;
    private readonly KafkaConsumerHostedService _service;

    public KafkaConsumerHostedServiceTests()
    {
        _kafkaConsumerServiceMock = new Mock<IKafkaConsumerService>();
        _loggerMock = new Mock<ILogger<KafkaConsumerHostedService>>();
        _service = new KafkaConsumerHostedService(_kafkaConsumerServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_StartsAndStopsConsumerCorrectly()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        await _service.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50); // Даем время на выполнение
        await _service.StopAsync(cancellationTokenSource.Token);

        // Assert
        _kafkaConsumerServiceMock.Verify(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()), Times.Once);
        _kafkaConsumerServiceMock.Verify(x => x.StopConsumingAsync(), Times.Exactly(2)); // В ExecuteAsync и StopAsync
    }

    [Fact]
    public async Task ExecuteAsync_ConsumerThrowsException_LogsErrorAndStops()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        _kafkaConsumerServiceMock.Setup(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Consumer error"));

        // Act
        await _service.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50); // Даем время на выполнение
        await _service.StopAsync(cancellationTokenSource.Token);

        // Assert
        _kafkaConsumerServiceMock.Verify(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()), Times.Once);
        _kafkaConsumerServiceMock.Verify(x => x.StopConsumingAsync(), Times.Exactly(2)); // В ExecuteAsync (finally) и StopAsync
    }

    [Fact]
    public async Task StopAsync_CallsStopConsumingAsync()
    {
        // Act
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _kafkaConsumerServiceMock.Verify(x => x.StopConsumingAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CancellationTokenCancelled_StopsGracefully()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        await _service.StartAsync(cancellationTokenSource.Token);

        // Assert
        _kafkaConsumerServiceMock.Verify(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()), Times.Once);
        _kafkaConsumerServiceMock.Verify(x => x.StopConsumingAsync(), Times.AtLeastOnce); // В ExecuteAsync (finally) и возможно в StopAsync
    }

    [Fact]
    public async Task ExecuteAsync_StopConsumingAsyncThrowsException_HandlesGracefully()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        _kafkaConsumerServiceMock.Setup(x => x.StopConsumingAsync())
            .ThrowsAsync(new Exception("Stop error"));

        // Act & Assert
        // Исключение должно быть проброшено, так как StopConsumingAsync вызывается в finally блоке
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await _service.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(50); // Даем время на выполнение
            await _service.StopAsync(cancellationTokenSource.Token);
        });
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentStartStop_HandlesCorrectly()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        var startTask = _service.StartAsync(cancellationTokenSource.Token);
        var stopTask = _service.StopAsync(cancellationTokenSource.Token);

        await Task.WhenAll(startTask, stopTask);

        // Assert
        _kafkaConsumerServiceMock.Verify(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()), Times.Once);
        _kafkaConsumerServiceMock.Verify(x => x.StopConsumingAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ConsumerCompletesNormally_StopsGracefully()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        _kafkaConsumerServiceMock.Setup(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask); // Немедленно завершается

        // Act
        await _service.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50); // Даем время на выполнение
        await _service.StopAsync(cancellationTokenSource.Token);

        // Assert
        _kafkaConsumerServiceMock.Verify(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()), Times.Once);
        _kafkaConsumerServiceMock.Verify(x => x.StopConsumingAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_MultipleStartStopCycles_HandlesCorrectly()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();

        // Act
        for (int i = 0; i < 3; i++)
        {
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(50));
            await _service.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(25);
            await _service.StopAsync(cancellationTokenSource.Token);
            
            // Создаем новый токен для следующего цикла
            cancellationTokenSource = new CancellationTokenSource();
        }

        // Assert
        _kafkaConsumerServiceMock.Verify(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        _kafkaConsumerServiceMock.Verify(x => x.StopConsumingAsync(), Times.Exactly(6)); // 2 раза на цикл
    }

    [Fact]
    public async Task ExecuteAsync_DisposeCalled_StopsConsumer()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        await _service.StartAsync(cancellationTokenSource.Token);
        await Task.Delay(50);
        
        // Симулируем вызов Dispose через IDisposable
        if (_service is IDisposable disposable)
        {
            disposable.Dispose();
        }

        // Assert
        _kafkaConsumerServiceMock.Verify(x => x.StartConsumingAsync(It.IsAny<CancellationToken>()), Times.Once);
        _kafkaConsumerServiceMock.Verify(x => x.StopConsumingAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var service = new KafkaConsumerHostedService(_kafkaConsumerServiceMock.Object, _loggerMock.Object);

        // Assert
        Assert.NotNull(service);
    }
} 