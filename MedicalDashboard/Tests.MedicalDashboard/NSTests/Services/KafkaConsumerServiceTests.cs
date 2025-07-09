using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Services.Consumer;

namespace Tests.MedicalDashboard.NSTests.Services
{
    public class KafkaConsumerServiceTests
    {
        private readonly Mock<IConsumer<string, string>> _consumerMock = new();
        private readonly Mock<ILogger<KafkaConsumerService<object>>> _loggerMock = new();
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock = new();
        private readonly KafkaConsumerService<object> _service;

        public KafkaConsumerServiceTests()
        {
            _service = new KafkaConsumerService<object>(_consumerMock.Object, _loggerMock.Object, _scopeFactoryMock.Object);
        }

        [Fact]
        public async Task StartAsync_LogsInfo()
        {
            await _service.StartAsync(CancellationToken.None);
            _loggerMock.Verify(l => l.Log(
                It.Is<LogLevel>(ll => ll == LogLevel.Information),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task StopAsync_LogsInfo()
        {
            await _service.StopAsync(CancellationToken.None);
            _loggerMock.Verify(l => l.Log(
                It.Is<LogLevel>(ll => ll == LogLevel.Information),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Dispose_LogsFailureOnException()
        {
            _consumerMock.Setup(c => c.Dispose()).Throws(new Exception("fail"));
            Assert.Throws<Exception>(() => _service.Dispose());
            _loggerMock.Verify(l => l.Log(
                It.Is<LogLevel>(ll => ll == LogLevel.Error),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()), Times.AtLeastOnce);
        }
    }
} 