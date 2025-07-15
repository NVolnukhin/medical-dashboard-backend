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
    public class SystolicPressureProcessorTests : IDisposable
    {
        protected readonly Mock<IGeneratorService> _generatorMock;
        protected readonly Mock<IKafkaService> _kafkaServiceMock;
        protected readonly Mock<ILogger<SystolicPressureProcessor>> _loggerMock;
        protected readonly SystolicPressureProcessor _processor;
        private readonly IOptions<MetricGenerationConfig> _config;

        public SystolicPressureProcessorTests()
        {
            _generatorMock = new Mock<IGeneratorService>();
            _kafkaServiceMock = new Mock<IKafkaService>();
            _loggerMock = new Mock<ILogger<SystolicPressureProcessor>>();

            _config = Options.Create(new MetricGenerationConfig
            {
                PressureIntervalSeconds = 60 
            });

            _processor = new SystolicPressureProcessor(
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
                new SystolicPressureProcessor(
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
            var patient = new Patient();
            const double expectedPressure = 120.0;

            _generatorMock
                .Setup(g => g.GenerateSystolicPressure())
                .Returns(expectedPressure);

            var result = await _processor.GenerateMetricValue(patient);

            Assert.Equal(expectedPressure, result);
            _generatorMock.Verify(
                g => g.GenerateSystolicPressure(),
                Times.Once);
        }

        [Fact]
        public void UpdatePatientMetric_UpdatesCorrectProperties()
        {
            var patient = new Patient
            {
                SystolicPressure = new Metric { Value = 110.0, LastUpdate = DateTime.UtcNow.AddDays(-1) }
            };
            const double newValue = 115.0;
            var beforeUpdate = DateTime.UtcNow;

            _processor.UpdatePatientMetric(patient, newValue);
            var afterUpdate = DateTime.UtcNow;

            Assert.Equal(newValue, patient.SystolicPressure.Value);
            Assert.True(patient.SystolicPressure.LastUpdate >= beforeUpdate);
            Assert.True(patient.SystolicPressure.LastUpdate <= afterUpdate);
        }

        [Fact]
        public async Task Update_WhenIntervalPassed_UpdatesPatient()
        {
            const double initialPressure = 110.0;
            const double newPressure = 115.0;
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                SystolicPressure = new Metric
                {
                    Value = initialPressure,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-70) // Больше интервала (60s)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "SystolicPressure", DateTime.UtcNow.AddSeconds(-70) }
                }
            };

            _generatorMock
                .Setup(g => g.GenerateSystolicPressure())
                .Returns(newPressure);

            _kafkaServiceMock
                .Setup(k => k.ProduceAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(newPressure, patient.SystolicPressure.Value);
            Assert.True((DateTime.UtcNow - patient.SystolicPressure.LastUpdate).TotalSeconds < 1);
            _kafkaServiceMock.Verify(
                k => k.SendToAllTopics(patient, "SystolicPressure", newPressure),
                Times.Once);
        }

        [Fact]
        public async Task Update_WhenIntervalNotPassed_SkipsPatient()
        {
            const double initialPressure = 110.0;
            var patient = new Patient
            {
                SystolicPressure = new Metric
                {
                    Value = initialPressure,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-50) // Меньше интервала (60s)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "SystolicPressure", DateTime.UtcNow.AddSeconds(-50) }
                }
            };

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(initialPressure, patient.SystolicPressure.Value);
            _generatorMock.Verify(
                g => g.GenerateSystolicPressure(),
                Times.Never);
        }

        [Fact]
        public async Task Update_WhenPatientsIsNull_LogsError()
        {
            // Arrange
            List<Patient> nullPatients = null!;
            var expectedMessage = "Ошибка в Update для SystolicPressure";

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
            const double expectedPressure = 120.0;
            var patient = new Patient
            {
                SystolicPressure = new Metric { Value = expectedPressure }
            };

            var result = _processor.GetMetricValue(patient);
            Assert.Equal(expectedPressure, result);
        }

        [Fact]
        public void GetUnit_ReturnsMmHg()
        {
            var unit = _processor.GetUnit();
            Assert.Equal("мм рт.ст.", unit);
        }

        [Fact]
        public void GenerateMetricValue_DoesNotUsePatientValue()
        {
            var patient1 = new Patient { SystolicPressure = new Metric { Value = 110.0 } };
            var patient2 = new Patient { SystolicPressure = new Metric { Value = 130.0 } };
            const double fixedPressure = 120.0;

            _generatorMock
                .Setup(g => g.GenerateSystolicPressure())
                .Returns(fixedPressure);
            var result1 = _processor.GenerateMetricValue(patient1).Result;
            var result2 = _processor.GenerateMetricValue(patient2).Result;

            Assert.Equal(fixedPressure, result1);
            Assert.Equal(fixedPressure, result2);
            _generatorMock.Verify(
                g => g.GenerateSystolicPressure(),
                Times.Exactly(2));
        }
    }
}