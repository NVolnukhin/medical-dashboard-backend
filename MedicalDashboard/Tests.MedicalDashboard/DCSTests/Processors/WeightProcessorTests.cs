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

namespace Tests.MedicalDashboard.DCSTests.Services
{
    public class WeightProcessorTests : IDisposable
    {
        protected readonly Mock<IGeneratorService> _generatorMock;
        protected readonly Mock<IKafkaService> _kafkaServiceMock;
        protected readonly Mock<ILogger<WeightProcessor>> _loggerMock;
        protected readonly WeightProcessor _processor;
        private readonly IOptions<MetricGenerationConfig> _config;

        public WeightProcessorTests()
        {
            _generatorMock = new Mock<IGeneratorService>();
            _kafkaServiceMock = new Mock<IKafkaService>();
            _loggerMock = new Mock<ILogger<WeightProcessor>>();

            _config = Options.Create(new MetricGenerationConfig
            {
                WeightIntervalSeconds = 300
            });

            _processor = new WeightProcessor(
                _generatorMock.Object,
                _kafkaServiceMock.Object,
                _config,
                _loggerMock.Object);
        }

        public void Dispose(){}

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
                new WeightProcessor(
                    _generatorMock.Object,
                    _kafkaServiceMock.Object,
                    nullConfig,
                    _loggerMock.Object));
        }

        [Fact]
        public void GetIntervalSeconds_ReturnsCorrectValue()
        {
            const int expectedInterval = 300;
            var result = _processor.GetIntervalSeconds();
            Assert.Equal(expectedInterval, result);
        }

        [Fact]
        public async Task GenerateMetricValue_CallsGeneratorCorrectly()
        {
            var patient = new Patient
            {
                Weight = new Metric { Value = 70.0 },
                BaseWeight = 75.0
            };
            const double expectedWeight = 71.5;

            _generatorMock
                .Setup(g => g.GenerateWeight(patient.Weight.Value, patient.BaseWeight))
                .Returns(expectedWeight);

            var result = await _processor.GenerateMetricValue(patient);

            Assert.Equal(expectedWeight, result);
            _generatorMock.Verify(
                g => g.GenerateWeight(patient.Weight.Value, patient.BaseWeight),
                Times.Once);
        }

        [Fact]
        public void UpdatePatientMetric_UpdatesCorrectProperties()
        {
            // Arrange
            var patient = new Patient
            {
                Weight = new Metric { Value = 70.0, LastUpdate = DateTime.UtcNow.AddDays(-1) }
            };
            const double newValue = 71.5;
            var beforeUpdate = DateTime.UtcNow;

            // Act
            _processor.UpdatePatientMetric(patient, newValue);
            var afterUpdate = DateTime.UtcNow;

            // Assert
            Assert.Equal(newValue, patient.Weight.Value);
            Assert.True(patient.Weight.LastUpdate >= beforeUpdate);
            Assert.True(patient.Weight.LastUpdate <= afterUpdate);
        }

        [Fact]
        public async Task Update_WhenIntervalPassed_UpdatesPatient()
        {
            const double initialWeight = 70.0;
            const double newWeight = 71.5;
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Weight = new Metric
                {
                    Value = initialWeight,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-400) // Больше интервала (300s)
                },
                BaseWeight = 75.0,
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "Weight", DateTime.UtcNow.AddSeconds(-400) }
                }
            };

            _generatorMock
                .Setup(g => g.GenerateWeight(initialWeight, patient.BaseWeight))
                .Returns(newWeight);

            _kafkaServiceMock
                .Setup(k => k.ProduceAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>())) // Явно указываем тип
                .Returns(Task.CompletedTask);

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(newWeight, patient.Weight.Value);
            Assert.True((DateTime.UtcNow - patient.Weight.LastUpdate).TotalSeconds < 1);
            _kafkaServiceMock.Verify(
                k => k.ProduceAsync(
                    "patient_metrics",
                    It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_WhenIntervalNotPassed_SkipsPatient()
        {
            // Arrange
            const double initialWeight = 70.0;
            var patient = new Patient
            {
                Weight = new Metric
                {
                    Value = initialWeight,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-200) // Меньше интервала (300s)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "Weight", DateTime.UtcNow.AddSeconds(-200) }
                }
            };

            // Act
            await _processor.Update(new List<Patient> { patient });

            // Assert
            Assert.Equal(initialWeight, patient.Weight.Value);
            _generatorMock.Verify(
                g => g.GenerateWeight(It.IsAny<double>(), It.IsAny<double>()),
                Times.Never);
        }

        [Fact]
        public async Task Update_WhenException_LogsError()
        {
            var patients = new List<Patient> { new Patient() }; // Невалидный пациент
            var expectedException = new InvalidOperationException("Test error");

            _generatorMock
                .Setup(g => g.GenerateWeight(It.IsAny<double>(), It.IsAny<double>()))
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
            const double expectedWeight = 68.5;
            var patient = new Patient
            {
                Weight = new Metric { Value = expectedWeight }
            };

            var result = _processor.GetMetricValue(patient);
            Assert.Equal(expectedWeight, result);
        }

        [Fact]
        public void GetUnit_ReturnsKg()
        {
            var unit = _processor.GetUnit();
            Assert.Equal("кг", unit);
        }
    }
}