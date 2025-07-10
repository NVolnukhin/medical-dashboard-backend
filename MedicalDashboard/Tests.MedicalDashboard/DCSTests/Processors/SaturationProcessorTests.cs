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
    public class SaturationProcessorTests : IDisposable
    {
        protected readonly Mock<IGeneratorService> _generatorMock;
        protected readonly Mock<IKafkaService> _kafkaServiceMock;
        protected readonly Mock<ILogger<SaturationProcessor>> _loggerMock;
        protected readonly SaturationProcessor _processor;
        private readonly IOptions<MetricGenerationConfig> _config;

        public SaturationProcessorTests()
        {
            _generatorMock = new Mock<IGeneratorService>();
            _kafkaServiceMock = new Mock<IKafkaService>();
            _loggerMock = new Mock<ILogger<SaturationProcessor>>();

            _config = Options.Create(new MetricGenerationConfig
            {
                SaturationIntervalSeconds = 30 
            });

            _processor = new SaturationProcessor(
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
                new SaturationProcessor(
                    _generatorMock.Object,
                    _kafkaServiceMock.Object,
                    nullConfig,
                    _loggerMock.Object));
        }

        [Fact]
        public void GetIntervalSeconds_ReturnsCorrectValue()
        {
            const int expectedInterval = 25;
            var result = _processor.GetIntervalSeconds();
            Assert.Equal(expectedInterval, result);
        }

        [Fact]
        public async Task GenerateMetricValue_CallsGeneratorCorrectly()
        {
            var patient = new Patient
            {
                Saturation = new Metric { Value = 95.0 }
            };
            const double expectedSaturation = 96.3;

            _generatorMock
                .Setup(g => g.GenerateSaturation(patient.Saturation.Value))
                .Returns(expectedSaturation);

            var result = await _processor.GenerateMetricValue(patient);

            Assert.Equal(expectedSaturation, result);
            _generatorMock.Verify(
                g => g.GenerateSaturation(patient.Saturation.Value),
                Times.Once);
        }

        [Fact]
        public void UpdatePatientMetric_UpdatesCorrectProperties()
        {
            var patient = new Patient
            {
                Saturation = new Metric { Value = 95.0, LastUpdate = DateTime.UtcNow.AddSeconds(-10) }
            };
            const double newValue = 97.0;
            var beforeUpdate = DateTime.UtcNow;

            _processor.UpdatePatientMetric(patient, newValue);
            var afterUpdate = DateTime.UtcNow;

            Assert.Equal(newValue, patient.Saturation.Value);
            Assert.True(patient.Saturation.LastUpdate >= beforeUpdate);
            Assert.True(patient.Saturation.LastUpdate <= afterUpdate);
        }

        [Fact]
        public async Task Update_WhenIntervalPassed_UpdatesPatient()
        {
            const double initialSaturation = 95.0;
            const double newSaturation = 96.5;
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Saturation = new Metric
                {
                    Value = initialSaturation,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-30)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "Saturation", DateTime.UtcNow.AddSeconds(-30) }
                }
            };

            _generatorMock
                .Setup(g => g.GenerateSaturation(initialSaturation))
                .Returns(newSaturation);

            _kafkaServiceMock
                .Setup(k => k.ProduceAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(newSaturation, patient.Saturation.Value);
            Assert.True((DateTime.UtcNow - patient.Saturation.LastUpdate).TotalSeconds < 1);
            _kafkaServiceMock.Verify(
                k => k.ProduceAsync("patient_metrics", It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_WhenIntervalNotPassed_SkipsPatient()
        {
            const double initialSaturation = 95.0;
            var patient = new Patient
            {
                Saturation = new Metric
                {
                    Value = initialSaturation,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-10)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "Saturation", DateTime.UtcNow.AddSeconds(-10) }
                }
            };

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(initialSaturation, patient.Saturation.Value);
            _generatorMock.Verify(
                g => g.GenerateSaturation(It.IsAny<double>()),
                Times.Never);
        }

        [Fact]
        public async Task Update_WhenException_LogsError()
        {
            var patients = new List<Patient> { new Patient() };
            var expectedException = new InvalidOperationException("Test error");

            _generatorMock
                .Setup(g => g.GenerateSaturation(It.IsAny<double>()))
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
            const double expectedSaturation = 94.7;
            var patient = new Patient
            {
                Saturation = new Metric { Value = expectedSaturation }
            };

            var result = _processor.GetMetricValue(patient);
            Assert.Equal(expectedSaturation, result);
        }

        [Fact]
        public void GetUnit_ReturnsPercentSymbol()
        {
            var unit = _processor.GetUnit();
            Assert.Equal("%", unit);
        }

        [Fact]
        public void GetMetricType_ReturnsSaturation()
        {
            var metricType = _processor.GetMetricType();
            Assert.Equal(MetricType.Saturation, metricType);
        }
    }
}