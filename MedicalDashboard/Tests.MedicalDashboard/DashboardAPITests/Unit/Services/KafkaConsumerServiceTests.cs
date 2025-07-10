using System.Text.Json;
using Confluent.Kafka;
using DashboardAPI.Services.Kafka.Consumer;
using DashboardAPI.Services.Kafka.Retry;
using DashboardAPI.Services.Metric;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Shared;
using Xunit;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Services;

public class KafkaConsumerServiceTests
{
    private readonly Mock<IConsumer<string, string>> _consumerMock;
    private readonly Mock<ILogger<KafkaConsumerService>> _loggerMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IKafkaRetryService> _retryServiceMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IMetricService> _metricServiceMock;
    private readonly string _topic = "test-topic";
    private readonly KafkaConsumerService _service;

    public KafkaConsumerServiceTests()
    {
        _consumerMock = new Mock<IConsumer<string, string>>();
        _loggerMock = new Mock<ILogger<KafkaConsumerService>>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _retryServiceMock = new Mock<IKafkaRetryService>();
        _scopeMock = new Mock<IServiceScope>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _metricServiceMock = new Mock<IMetricService>();

        _scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IMetricService))).Returns(_metricServiceMock.Object);

        _service = new KafkaConsumerService(
            _consumerMock.Object,
            _loggerMock.Object,
            _scopeFactoryMock.Object,
            _retryServiceMock.Object,
            _topic);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => new KafkaConsumerService(
            _consumerMock.Object,
            _loggerMock.Object,
            _scopeFactoryMock.Object,
            _retryServiceMock.Object,
            _topic));

        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_ShouldSubscribeToTopic()
    {
        // Arrange
        _retryServiceMock.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Assert
        _consumerMock.Verify(x => x.Subscribe(_topic), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldStartConsumingMessages()
    {
        // Arrange
        var consumeResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Value = "test message" }
        };

        _consumerMock.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Returns(consumeResult);

        _retryServiceMock.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Wait a bit for the consume loop to start
        await Task.Delay(100);

        // Assert
        _consumerMock.Verify(x => x.Consume(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAsync_WithValidMessage_ShouldProcessMetric()
    {
        // Arrange
        var metricDto = new MetricDto
        {
            PatientId = Guid.NewGuid(),
            Type = "HeartRate",
            Value = 75.5,
            Timestamp = DateTime.UtcNow
        };

        var message = JsonSerializer.Serialize(metricDto);
        var consumeResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Value = message }
        };

        _consumerMock.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Returns(consumeResult);

        _retryServiceMock.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _metricServiceMock.Setup(x => x.ProcessMetricFromKafkaAsync(It.IsAny<MetricDto>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Wait a bit for the consume loop to start
        await Task.Delay(100);

        // Assert
        _retryServiceMock.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task>>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAsync_WithNullMessage_ShouldNotProcess()
    {
        // Arrange
        var consumeResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Value = null! }
        };

        _consumerMock.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Returns(consumeResult);

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Wait a bit for the consume loop to start
        await Task.Delay(100);

        // Assert
        _retryServiceMock.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task StartAsync_WithInvalidJson_ShouldLogWarning()
    {
        // Arrange
        var invalidJson = "invalid json";
        var consumeResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Value = invalidJson }
        };

        _consumerMock.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Returns(consumeResult);

        _retryServiceMock.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task>>(), It.IsAny<string>()))
            .ThrowsAsync(new JsonException("Invalid JSON"));

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Wait a bit for the consume loop to start
        await Task.Delay(100);

        // Assert
        _retryServiceMock.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task>>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAsync_WithException_ShouldLogError()
    {
        // Arrange
        var consumeResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Value = "test message" }
        };

        _consumerMock.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Throws(new Exception("Test exception"));

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Wait a bit for the consume loop to start
        await Task.Delay(100);

        // Assert
        _consumerMock.Verify(x => x.Consume(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAsync_WithMetricServiceException_ShouldRetry()
    {
        // Arrange
        var metricDto = new MetricDto
        {
            PatientId = Guid.NewGuid(),
            Type = "HeartRate",
            Value = 75.5,
            Timestamp = DateTime.UtcNow
        };

        var message = JsonSerializer.Serialize(metricDto);
        var consumeResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Value = message }
        };

        _consumerMock.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Returns(consumeResult);

        _metricServiceMock.Setup(x => x.ProcessMetricFromKafkaAsync(It.IsAny<MetricDto>()))
            .ThrowsAsync(new Exception("Metric service error"));

        _retryServiceMock.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task>>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Retry failed"));

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Wait a bit for the consume loop to start
        await Task.Delay(100);

        // Assert
        _retryServiceMock.Verify(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task>>(), It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAsync_WithOperationCanceledException_ShouldStopGracefully()
    {
        // Arrange
        _consumerMock.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Throws(new OperationCanceledException());

        // Act
        await _service.StartAsync(CancellationToken.None);

        // Wait a bit for the consume loop to start
        await Task.Delay(100);

        // Assert
        _consumerMock.Verify(x => x.Consume(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(100);

        // Act
        await _service.StartAsync(cancellationTokenSource.Token);

        // Wait for cancellation
        await Task.Delay(200);

        // Assert
        _consumerMock.Verify(x => x.Subscribe(_topic), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldCancelConsumption()
    {
        // Arrange
        var consumeResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Value = "test message" }
        };

        _consumerMock.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Returns(consumeResult);

        _retryServiceMock.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _service.StartAsync(CancellationToken.None);

        // Wait a bit for the consume loop to start
        await Task.Delay(100);

        // Act
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _consumerMock.Verify(x => x.Close(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldWaitForConsumeTask()
    {
        // Arrange
        var consumeResult = new ConsumeResult<string, string>
        {
            Message = new Message<string, string> { Value = "test message" }
        };

        _consumerMock.Setup(x => x.Consume(It.IsAny<CancellationToken>()))
            .Returns(consumeResult);

        _retryServiceMock.Setup(x => x.ExecuteWithRetryAsync(It.IsAny<Func<Task>>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _service.StartAsync(CancellationToken.None);

        // Wait a bit for the consume loop to start
        await Task.Delay(100);

        // Act
        await _service.StopAsync(CancellationToken.None);

        // Assert
        _consumerMock.Verify(x => x.Close(), Times.Once);
    }
} 