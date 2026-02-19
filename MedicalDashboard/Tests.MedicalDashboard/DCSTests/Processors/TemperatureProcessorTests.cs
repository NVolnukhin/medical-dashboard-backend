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
        public async Task Update_IntervalPassed_PatientProcessed()
        {
            var now = DateTime.UtcNow;
            const double initialTemperature = 36.6;
            var id = Guid.NewGuid();
            var patient = new Patient
            {
                Id = id,
                Name = "Test Patient",
                Temperature = new Metric { Value = initialTemperature },
                MetricLastGenerations = new Dictionary<string, DateTime>
        {
            { "Temperature", now.AddSeconds(-61) }
        }
            };

            var patients = new List<Patient> { patient };
            const double generatedTemperature = 37.1;

            _generatorMock
                .Setup(x => x.GenerateTemperature(It.IsAny<double?>()))
                .Returns(generatedTemperature);

            await _processor.Update(patients);
            _generatorMock.Verify(
                x => x.GenerateTemperature(initialTemperature),
                Times.Once);
            Assert.Equal(generatedTemperature, patient.Temperature.Value);
            Assert.Equal(now, patient.MetricLastGenerations["Temperature"], TimeSpan.FromMilliseconds(100));

            _kafkaServiceMock.Verify(
                x => x.SendToAllTopics(patient, "Temperature", generatedTemperature),
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
                { "Temperature", now.AddSeconds(-25) } // Интервал 30 сек, прошло 25
            }
        }
    };
            await _processor.Update(patients);

            _generatorMock.Verify(
                x => x.GenerateTemperature(It.IsAny<double?>()),
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
            var expectedMessage = "Ошибка в Update для Temperature";

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