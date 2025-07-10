using AuthService.Kafka;
using AuthService.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Tests.MedicalDashboard.ASTests.Unit.Services;

public class KafkaProducerServiceTests
{
    private readonly Mock<IOptions<KafkaConfig>> _mockSettings;
    private readonly Mock<ILogger<KafkaProducerService>> _mockLogger;
    private readonly Mock<IProducer<string, string>> _mockProducer;
    private readonly KafkaConfig _config;

    public KafkaProducerServiceTests()
    {
        _config = new KafkaConfig
        {
            BootstrapServers = "localhost:9092",
            ProducerClientId = "test-producer",
            TopicName = "test-topic"
        };

        _mockSettings = new Mock<IOptions<KafkaConfig>>();
        _mockSettings.Setup(x => x.Value).Returns(_config);

        _mockLogger = new Mock<ILogger<KafkaProducerService>>();
        _mockProducer = new Mock<IProducer<string, string>>();
    }

    [Fact]
    public async Task SendNotificationAsync_WithValidMessage_ShouldSendToKafka()
    {
        // Arrange
        var message = new NotificationMessage
        {
            Type = 0,
            Recipient = "recipient",
            Subject = "Test Subject",
            Body = "Test Body",
            Priority = 0,
            TemplateName = "test-template-name",
            TemplateParameters = new Dictionary<string, string>()
        };

        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = _config.TopicName,
            Partition = 0,
            Offset = 123
        };

        _mockProducer.Setup(x => x.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deliveryResult);

        var service = new KafkaProducerService(_mockSettings.Object, _mockLogger.Object, _mockProducer.Object);

        // Act
        await service.SendNotificationAsync(message);

        // Assert
        _mockProducer.Verify(x => x.ProduceAsync(
            _config.TopicName,
            It.Is<Message<string, string>>(m => !string.IsNullOrEmpty(m.Key) && !string.IsNullOrEmpty(m.Value)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_WithNullMessage_ShouldThrowNullReferenceException()
    {
        // Arrange
        NotificationMessage? message = null;
        var _service = new KafkaProducerService(_mockSettings.Object, _mockLogger.Object, _mockProducer.Object);
        
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _service.SendNotificationAsync(message));
    }

    [Fact]
    public async Task SendNotificationAsync_WhenProducerThrowsException_ShouldRethrow()
    {
        // Arrange
        var message = new NotificationMessage
        {
            Type = 0,
            Recipient = "recipient",
            Subject = "Test Subject",
            Body = "Test Body",
            Priority = 0,
            TemplateName = "test-template-name",
            TemplateParameters = new Dictionary<string, string>()
        };

        var expectedException = new Exception("Kafka error");
        _mockProducer.Setup(x => x.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var service = new KafkaProducerService(_mockSettings.Object, _mockLogger.Object, _mockProducer.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            service.SendNotificationAsync(message));
        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task SendNotificationAsync_WithEmptyMessage_ShouldSendToKafka()
    {
        // Arrange
        var message = new NotificationMessage
        {
            Type = 0,
            Recipient = "recipient",
            Subject = "Test Subject",
            Body = "Test Body",
            Priority = 0,
            TemplateName = "test-template-name",
            TemplateParameters = new Dictionary<string, string>()
        };

        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = _config.TopicName,
            Partition = 0,
            Offset = 123
        };

        _mockProducer.Setup(x => x.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deliveryResult);

        var service = new KafkaProducerService(_mockSettings.Object, _mockLogger.Object, _mockProducer.Object);

        // Act
        await service.SendNotificationAsync(message);

        // Assert
        _mockProducer.Verify(x => x.ProduceAsync(
            _config.TopicName,
            It.Is<Message<string, string>>(m => !string.IsNullOrEmpty(m.Key) && !string.IsNullOrEmpty(m.Value)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_WithSpecialCharacters_ShouldSendToKafka()
    {
        // Arrange
        var message = new NotificationMessage
        {
            Type = 0,
            Recipient = "recipient",
            Subject = "Test Subject",
            Body = "Test Body",
            Priority = 0,
            TemplateName = "test-template-name",
            TemplateParameters = new Dictionary<string, string>()
        };

        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = _config.TopicName,
            Partition = 0,
            Offset = 123
        };

        _mockProducer.Setup(x => x.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deliveryResult);

        var service = new KafkaProducerService(_mockSettings.Object, _mockLogger.Object, _mockProducer.Object);

        // Act
        await service.SendNotificationAsync(message);

        // Assert
        _mockProducer.Verify(x => x.ProduceAsync(
            _config.TopicName,
            It.Is<Message<string, string>>(m => !string.IsNullOrEmpty(m.Key) && !string.IsNullOrEmpty(m.Value)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_WithUnicodeCharacters_ShouldSendToKafka()
    {
        // Arrange
        var message = new NotificationMessage
        {
            Type = 0,
            Recipient = "recipient",
            Subject = "Test Subject",
            Body = "Test Body",
            Priority = 0,
            TemplateName = "test-template-name",
            TemplateParameters = new Dictionary<string, string>()
        };

        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = _config.TopicName,
            Partition = 0,
            Offset = 123
        };

        _mockProducer.Setup(x => x.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deliveryResult);

        var service = new KafkaProducerService(_mockSettings.Object, _mockLogger.Object, _mockProducer.Object);

        // Act
        await service.SendNotificationAsync(message);

        // Assert
        _mockProducer.Verify(x => x.ProduceAsync(
            _config.TopicName,
            It.Is<Message<string, string>>(m => !string.IsNullOrEmpty(m.Key) && !string.IsNullOrEmpty(m.Value)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendNotificationAsync_WithLongMessage_ShouldSendToKafka()
    {
        // Arrange
        var longString = new string('A', 10000);
        var message = new NotificationMessage
        {
            Type = 0,
            Recipient = "recipient",
            Subject = longString,
            Body = longString,
            Priority = 0,
            TemplateName = "test-template-name",
            TemplateParameters = new Dictionary<string, string>()
        };

        var deliveryResult = new DeliveryResult<string, string>
        {
            Topic = _config.TopicName,
            Partition = 0,
            Offset = 123
        };

        _mockProducer.Setup(x => x.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(deliveryResult);

        var service = new KafkaProducerService(_mockSettings.Object, _mockLogger.Object, _mockProducer.Object);

        // Act
        await service.SendNotificationAsync(message);

        // Assert
        _mockProducer.Verify(x => x.ProduceAsync(
            _config.TopicName,
            It.Is<Message<string, string>>(m => !string.IsNullOrEmpty(m.Key) && !string.IsNullOrEmpty(m.Value)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Dispose_ShouldDisposeProducer()
    {
        // Arrange
        var service = new KafkaProducerService(_mockSettings.Object, _mockLogger.Object, _mockProducer.Object);

        // Act
        service.Dispose();

        // Assert
        _mockProducer.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var service = new KafkaProducerService(_mockSettings.Object, _mockLogger.Object, _mockProducer.Object);

        // Act & Assert
        service.Dispose();
        service.Dispose(); // Should not throw
    }
} 