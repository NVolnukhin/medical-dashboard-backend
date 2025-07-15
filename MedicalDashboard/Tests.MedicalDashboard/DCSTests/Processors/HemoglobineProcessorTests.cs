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
    public class HemoglobinProcessorTests : IDisposable
    {
        protected readonly Mock<IGeneratorService> _generatorMock;
        protected readonly Mock<IKafkaService> _kafkaServiceMock;
        protected readonly Mock<ILogger<HemoglobinProcessor>> _loggerMock;
        protected readonly HemoglobinProcessor _processor;
        private readonly IOptions<MetricGenerationConfig> _config;

        public HemoglobinProcessorTests()
        {
            _generatorMock = new Mock<IGeneratorService>();
            _kafkaServiceMock = new Mock<IKafkaService>();
            _loggerMock = new Mock<ILogger<HemoglobinProcessor>>();

            _config = Options.Create(new MetricGenerationConfig
            {
                HemoglobinIntervalSeconds = 60 
            });

            _processor = new HemoglobinProcessor(
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
                new HemoglobinProcessor(
                    _generatorMock.Object,
                    _kafkaServiceMock.Object,
                    nullConfig,
                    _loggerMock.Object));
        }

        [Fact]
        public void GetIntervalSeconds_ReturnsCorrectValue()
        {
            const int expectedInterval = 60;
            var result = _processor.GetIntervalSeconds();
            Assert.Equal(expectedInterval, result);
        }

        [Fact]
        public async Task GenerateMetricValue_CallsGeneratorCorrectly()
        {
            var patient = new Patient
            {
                Hemoglobin = new Metric { Value = 140.0 }
            };
            const double expectedHemoglobin = 142.5;

            _generatorMock
                .Setup(g => g.GenerateHemoglobin(patient.Hemoglobin.Value))
                .Returns(expectedHemoglobin);

            var result = await _processor.GenerateMetricValue(patient);

            Assert.Equal(expectedHemoglobin, result);
            _generatorMock.Verify(
                g => g.GenerateHemoglobin(patient.Hemoglobin.Value),
                Times.Once);
        }

        [Fact]
        public void UpdatePatientMetric_UpdatesCorrectProperties()
        {
            var patient = new Patient
            {
                Hemoglobin = new Metric { Value = 135.0, LastUpdate = DateTime.UtcNow.AddSeconds(-10) }
            };
            const double newValue = 137.0;
            var beforeUpdate = DateTime.UtcNow;

            _processor.UpdatePatientMetric(patient, newValue);
            var afterUpdate = DateTime.UtcNow;

            Assert.Equal(newValue, patient.Hemoglobin.Value);
            Assert.True(patient.Hemoglobin.LastUpdate >= beforeUpdate);
            Assert.True(patient.Hemoglobin.LastUpdate <= afterUpdate);
        }

        [Fact]
        public async Task Update_WhenIntervalPassed_ProcessesPatient()
        {
            var now = DateTime.UtcNow;
            var id = Guid.NewGuid();

            string metricName = _processor.GetMetricType().ToString();

            var patient = new Patient
            {
                Id = id,
                Name = "Test Patient",
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    [metricName] = now.AddSeconds(-61)
                },
                Hemoglobin = new Metric { Value = 135 }
            };
            var patients = new List<Patient> { patient };

            double generatedValue = 136;
            _generatorMock.Setup(g => g.GenerateHemoglobin(patient.Hemoglobin.Value)).Returns(generatedValue);

            await _processor.Update(patients);
            _generatorMock.Verify(g => g.GenerateHemoglobin(patient.Hemoglobin.Value), Times.Once);

            Assert.Equal(generatedValue, patient.Hemoglobin.Value);
            Assert.InRange(patient.Hemoglobin.LastUpdate, now.AddSeconds(-1), now.AddSeconds(1));
            Assert.InRange(patient.MetricLastGenerations[metricName], now.AddSeconds(-1), now.AddSeconds(1));
            _kafkaServiceMock.Verify(
                k => k.SendToAllTopics(patient, metricName, generatedValue),
                Times.Once);
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Generated {metricName} for {patient.Name}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_WhenIntervalNotPassed_SkipsPatient()
        {
            const double initialHemoglobin = 140.0;
            var patient = new Patient
            {
                Hemoglobin = new Metric
                {
                    Value = initialHemoglobin,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-30)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "Hemoglobin", DateTime.UtcNow.AddSeconds(-30) }
                }
            };

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(initialHemoglobin, patient.Hemoglobin.Value);
            _generatorMock.Verify(
                g => g.GenerateHemoglobin(It.IsAny<double>()),
                Times.Never);
        }

        [Fact]
        public async Task Update_WhenPatientsIsNull_LogsError()
        {
            List<Patient> nullPatients = null!;
            var expectedMessage = "Ошибка в Update для Hemoglobin";

            await _processor.Update(nullPatients!);

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
            const double expectedHemoglobin = 145.0;
            var patient = new Patient
            {
                Hemoglobin = new Metric { Value = expectedHemoglobin }
            };

            var result = _processor.GetMetricValue(patient);
            Assert.Equal(expectedHemoglobin, result);
        }

        [Fact]
        public void GetUnit_ReturnsGramsPerLiter()
        {
            var unit = _processor.GetUnit();
            Assert.Equal("г/л", unit);
        }

        [Fact]
        public void GetMetricType_ReturnsHemoglobin()
        {
            var metricType = _processor.GetMetricType();
            Assert.Equal(MetricType.Hemoglobin, metricType);
        }
    }
}