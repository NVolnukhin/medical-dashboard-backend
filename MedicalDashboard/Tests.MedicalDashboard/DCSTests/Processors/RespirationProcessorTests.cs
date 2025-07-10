using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using DataCollectorService.Models;
using DataCollectorService.Processors;
using DataCollectorService.Services;
using DataCollectorService.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;

namespace Tests.MedicalDashboard.DCSTests.Processors
{
    public class RespirationProcessorTests : IDisposable
    {
        protected readonly Mock<IGeneratorService> _generatorMock;
        protected readonly Mock<IKafkaService> _kafkaServiceMock;
        protected readonly Mock<ILogger<RespirationProcessor>> _loggerMock;
        protected readonly RespirationProcessor _processor;
        private readonly IOptions<MetricGenerationConfig> _config;

        public RespirationProcessorTests()
        {
            _generatorMock = new Mock<IGeneratorService>();
            _kafkaServiceMock = new Mock<IKafkaService>();
            _loggerMock = new Mock<ILogger<RespirationProcessor>>();

            _config = Options.Create(new MetricGenerationConfig
            {
                RespirationIntervalSeconds = 60 
            });

            _processor = new RespirationProcessor(
                _generatorMock.Object,
                _kafkaServiceMock.Object,
                _config,
                _loggerMock.Object);
        }

        public void Dispose() {}

        [Fact]
        public void Constructor_ValidParameters_CreatesInstance()
        {
            Assert.NotNull(_processor);
        }

        [Fact]
        public void Constructor_NullConfig_ThrowsException()
        {
            var nullConfig = Options.Create<MetricGenerationConfig>(null);

            Assert.Throws<ArgumentNullException>(() =>
                new RespirationProcessor(
                    _generatorMock.Object,
                    _kafkaServiceMock.Object,
                    nullConfig,
                    _loggerMock.Object));
        }

        [Fact]
        public void GetIntervalSeconds_ReturnsCorrectValue()
        {
            const int expectedInterval = 40;
            var result = _processor.GetIntervalSeconds();
            Assert.Equal(expectedInterval, result);
        }

        [Fact]
        public async Task GenerateMetricValue_CallsGeneratorCorrectly()
        {
            var patient = new Patient
            {
                RespirationRate = new Metric { Value = 16.0 }
            };
            const double expectedRespiration = 17.5;

            _generatorMock
                .Setup(g => g.GenerateRespiration(patient.RespirationRate.Value))
                .Returns(expectedRespiration);

            var result = await _processor.GenerateMetricValue(patient);

            Assert.Equal(expectedRespiration, result);
            _generatorMock.Verify(
                g => g.GenerateRespiration(patient.RespirationRate.Value),
                Times.Once);
        }

        [Fact]
        public void UpdatePatientMetric_UpdatesCorrectProperties()
        {
            var patient = new Patient
            {
                RespirationRate = new Metric { Value = 16.0, LastUpdate = DateTime.UtcNow.AddSeconds(-10) }
            };
            const double newValue = 18.0;
            var beforeUpdate = DateTime.UtcNow;

            _processor.UpdatePatientMetric(patient, newValue);
            var afterUpdate = DateTime.UtcNow;

            Assert.Equal(newValue, patient.RespirationRate.Value);
            Assert.True(patient.RespirationRate.LastUpdate >= beforeUpdate);
            Assert.True(patient.RespirationRate.LastUpdate <= afterUpdate);
        }

        [Fact]
        public async Task Update_WhenIntervalPassed_UpdatesPatient()
        {
            const double initialRespiration = 16.0;
            const double newRespiration = 17.5;
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                RespirationRate = new Metric
                {
                    Value = initialRespiration,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-45)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "RespirationRate", DateTime.UtcNow.AddSeconds(-45) }
                }
            };

            _generatorMock
                .Setup(g => g.GenerateRespiration(initialRespiration))
                .Returns(newRespiration);

            _kafkaServiceMock
                .Setup(k => k.ProduceAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(newRespiration, patient.RespirationRate.Value);
            Assert.True((DateTime.UtcNow - patient.RespirationRate.LastUpdate).TotalSeconds < 1);
            _kafkaServiceMock.Verify(
                k => k.ProduceAsync("patient_metrics", It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_WhenIntervalNotPassed_SkipsPatient()
        {
            const double initialRespiration = 16.0;
            var patient = new Patient
            {
                RespirationRate = new Metric
                {
                    Value = initialRespiration,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-20)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "RespirationRate", DateTime.UtcNow.AddSeconds(-20) }
                }
            };

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(initialRespiration, patient.RespirationRate.Value);
            _generatorMock.Verify(
                g => g.GenerateRespiration(It.IsAny<double>()),
                Times.Never);
        }

        [Fact]
        public async Task Update_WhenException_LogsError()
        {
            var patients = new List<Patient> { new Patient() };
            var expectedException = new InvalidOperationException("Test error");

            _generatorMock
                .Setup(g => g.GenerateRespiration(It.IsAny<double>()))
                .Throws(expectedException);

            await _processor.Update(patients);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(expectedException.Message)),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void GetMetricValue_ReturnsCorrectValue()
        {
            const double expectedRespiration = 18.0;
            var patient = new Patient
            {
                RespirationRate = new Metric { Value = expectedRespiration }
            };

            var result = _processor.GetMetricValue(patient);
            Assert.Equal(expectedRespiration, result);
        }

        [Fact]
        public void GetUnit_ReturnsBreathsPerMinute()
        {
            var unit = _processor.GetUnit();
            Assert.Equal("вдохов/мин", unit);
        }

        [Fact]
        public void GetMetricType_ReturnsRespirationRate()
        {
            var metricType = _processor.GetMetricType();
            Assert.Equal(MetricType.RespirationRate, metricType);
        }
    }
}