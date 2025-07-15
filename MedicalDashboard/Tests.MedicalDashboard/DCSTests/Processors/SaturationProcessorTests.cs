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
            const int expectedInterval = 30;
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
        public async Task Update_IntervalPassed_PatientProcessed()
        {
            var now = DateTime.UtcNow;
            const double initialSaturation = 97.5; 
            var id = Guid.NewGuid();
            var patient = new Patient
            {
                Id = id,
                Name = "Test Patient",
                Saturation = new Metric { Value = initialSaturation },
                MetricLastGenerations = new Dictionary<string, DateTime>
        {
            { "Saturation", now.AddSeconds(-31) }
        }
            };

            var patients = new List<Patient> { patient };
            const double generatedSaturation = 98.2;

            _generatorMock
                .Setup(x => x.GenerateSaturation(It.IsAny<double?>()))
                .Returns(generatedSaturation);

            await _processor.Update(patients);
            _generatorMock.Verify(
                x => x.GenerateSaturation(initialSaturation),
                Times.Once);
            Assert.Equal(generatedSaturation, patient.Saturation.Value);
            Assert.Equal(now, patient.MetricLastGenerations["Saturation"], TimeSpan.FromMilliseconds(100));

            _kafkaServiceMock.Verify(
                x => x.SendToAllTopics(patient, "Saturation", generatedSaturation),
                Times.Once);
        }

        [Fact]
        public async Task Update_IntervalNotPassed_PatientSkipped()
        {
            var now = DateTime.UtcNow;
            var patients = new List<Patient>
    {
        new Patient
        {
            MetricLastGenerations = new Dictionary<string, DateTime>
            {
                { "Saturation", now.AddSeconds(-25) } // Интервал 30 сек, прошло 25
            }
        }
    };
            await _processor.Update(patients);
            _generatorMock.Verify(
                x => x.GenerateSaturation(It.IsAny<double?>()),
                Times.Never);

            _kafkaServiceMock.Verify(
                x => x.SendToAllTopics(It.IsAny<Patient>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Never);

            _loggerMock.Verify(
                x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task Update_WhenPatientsIsNull_LogsError()
        {
            // Arrange
            List<Patient> nullPatients = null!;
            var expectedMessage = "Ошибка в Update для Saturation";

            // Act
            await _processor.Update(nullPatients!);

            // Assert
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains(expectedMessage)),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
                Times.Once
            );
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