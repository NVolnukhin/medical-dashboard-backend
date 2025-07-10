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
    public class TemperatureProcessorTests : IDisposable
    {
        protected readonly Mock<IGeneratorService> _generatorMock;
        protected readonly Mock<IKafkaService> _kafkaServiceMock;
        protected readonly Mock<ILogger<TemperatureProcessor>> _loggerMock;
        protected readonly TemperatureProcessor _processor;
        private readonly IOptions<MetricGenerationConfig> _config;

        public TemperatureProcessorTests()
        {
            _generatorMock = new Mock<IGeneratorService>();
            _kafkaServiceMock = new Mock<IKafkaService>();
            _loggerMock = new Mock<ILogger<TemperatureProcessor>>();

            _config = Options.Create(new MetricGenerationConfig
            {
                TemperatureIntervalSeconds = 60 
            });

            _processor = new TemperatureProcessor(
                _generatorMock.Object,
                _kafkaServiceMock.Object,
                _config,
                _loggerMock.Object);
        }

        public void Dispose() { }

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
                new TemperatureProcessor(
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
                Temperature = new Metric { Value = 36.6 }
            };
            const double expectedTemperature = 36.8;

            _generatorMock
                .Setup(g => g.GenerateTemperature(patient.Temperature.Value))
                .Returns(expectedTemperature);

            var result = await _processor.GenerateMetricValue(patient);

            Assert.Equal(expectedTemperature, result);
            _generatorMock.Verify(
                g => g.GenerateTemperature(patient.Temperature.Value),
                Times.Once);
        }

        [Fact]
        public void UpdatePatientMetric_UpdatesCorrectProperties()
        {
            var patient = new Patient
            {
                Temperature = new Metric { Value = 36.6, LastUpdate = DateTime.UtcNow.AddDays(-1) }
            };
            const double newValue = 37.1;
            var beforeUpdate = DateTime.UtcNow;

            _processor.UpdatePatientMetric(patient, newValue);
            var afterUpdate = DateTime.UtcNow;

            Assert.Equal(newValue, patient.Temperature.Value);
            Assert.True(patient.Temperature.LastUpdate >= beforeUpdate);
            Assert.True(patient.Temperature.LastUpdate <= afterUpdate);
        }

        [Fact]
        public async Task Update_WhenIntervalPassed_UpdatesPatient()
        {
            const double initialTemperature = 36.6;
            const double newTemperature = 37.1;
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Temperature = new Metric
                {
                    Value = initialTemperature,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-70) // Больше интервала (60s)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "Temperature", DateTime.UtcNow.AddSeconds(-70) }
                }
            };

            _generatorMock
                .Setup(g => g.GenerateTemperature(initialTemperature))
                .Returns(newTemperature);

            _kafkaServiceMock
                .Setup(k => k.ProduceAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(newTemperature, patient.Temperature.Value);
            Assert.True((DateTime.UtcNow - patient.Temperature.LastUpdate).TotalSeconds < 1);
            _kafkaServiceMock.Verify(
                k => k.ProduceAsync("patient_metrics", It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_WhenIntervalNotPassed_SkipsPatient()
        {
            const double initialTemperature = 36.6;
            var patient = new Patient
            {
                Temperature = new Metric
                {
                    Value = initialTemperature,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-50) // Меньше интервала (60s)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "Temperature", DateTime.UtcNow.AddSeconds(-50) }
                }
            };

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(initialTemperature, patient.Temperature.Value);
            _generatorMock.Verify(
                g => g.GenerateTemperature(It.IsAny<double>()),
                Times.Never);
        }

        [Fact]
        public async Task Update_WhenException_LogsError()
        {
            var patients = new List<Patient> { new Patient() };
            var expectedException = new InvalidOperationException("Test error");

            _generatorMock
                .Setup(g => g.GenerateTemperature(It.IsAny<double>()))
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
            const double expectedTemperature = 37.2;
            var patient = new Patient
            {
                Temperature = new Metric { Value = expectedTemperature }
            };

            var result = _processor.GetMetricValue(patient);
            Assert.Equal(expectedTemperature, result);
        }

        [Fact]
        public void GetUnit_ReturnsCelsius()
        {
            var unit = _processor.GetUnit();
            Assert.Equal("°C", unit);
        }
    }
}