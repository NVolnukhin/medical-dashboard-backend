using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using DataCollectorService.Kafka;
using DataCollectorService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shared;
using Xunit;

namespace Tests.MedicalDashboard.DCSTests.Kafka
{
    public class KafkaServiceTests : IDisposable
    {
        private readonly Mock<ILogger<KafkaService>> _loggerMock;
        private readonly Mock<IProducer<string, string>> _producerMock;
        private readonly KafkaService _kafkaService;
        private readonly KafkaConfig _config;

        public KafkaServiceTests()
        {
            _loggerMock = new Mock<ILogger<KafkaService>>();
            _producerMock = new Mock<IProducer<string, string>>();

            _config = new KafkaConfig
            {
                BootstrapServers = "localhost:9092",
                RawInformationTopic = "raw-info",
                MetricsTopic = "metrics",
                ClientId = "test-client",
                Acks = "All",
                MessageTimeoutMs = 5000
            };

            var options = Options.Create(_config);

            // Создаем реальный KafkaService, но подменим его внутренний producer через reflection
            _kafkaService = new KafkaService(options, _loggerMock.Object);

            // Подмена внутреннего producer через reflection
            var producerField = typeof(KafkaService).GetField("_producer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            producerField.SetValue(_kafkaService, _producerMock.Object);
        }

        public void Dispose()
        {
            _kafkaService.Dispose();
        }

        [Fact]
        public void Constructor_ValidConfig_CreatesInstance()
        {
            var options = Options.Create(_config);
            var service = new KafkaService(options, _loggerMock.Object);
            Assert.NotNull(service);
        }

        [Fact]
        public async Task SendToAllTopics_ValidData_SendsToBothTopics()
        {
            var patient = new Patient { Id = Guid.NewGuid() };
            const string metricName = "Pulse";
            const double value = 72.0;

            _producerMock.Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new DeliveryResult<string, string>()));

            await _kafkaService.SendToAllTopics(patient, metricName, value);

            _producerMock.Verify(p => p.ProduceAsync(
                _config.RawInformationTopic,
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()),
                Times.Once);

            _producerMock.Verify(p => p.ProduceAsync(
                _config.MetricsTopic,
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ProduceAsync_ValidData_SendsMessage()
        {
            const string key = "test-key";
            const string message = "test-message";

            _producerMock.Setup(p => p.ProduceAsync(
                _config.RawInformationTopic,
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new DeliveryResult<string, string>()));
            
            await _kafkaService.ProduceAsync(key, message);

            _producerMock.Verify(p => p.ProduceAsync(
                _config.RawInformationTopic,
                It.Is<Message<string, string>>(m =>
                    m.Key == key && m.Value == message),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendToKafka_ProducerError_LogsError()
        {
            var patient = new Patient { Id = Guid.NewGuid() };
            const string metricName = "Temperature";
            const double value = 36.6;
            var expectedException = new ProduceException<string, string>(
                new Error(ErrorCode.Local_Fail),
                new DeliveryResult<string, string>());

            _producerMock.Setup(p => p.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<Message<string, string>>(),
                It.IsAny<CancellationToken>()))
                .Throws(expectedException);

            await _kafkaService.SendToKafka(patient, metricName, value);

            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(metricName)),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void Dispose_FlushesAndDisposesProducer()
        {
            _kafkaService.Dispose();

            _producerMock.Verify(p => p.Flush(It.IsAny<TimeSpan>()), Times.Once);
            _producerMock.Verify(p => p.Dispose(), Times.Once);
        }
    }
}