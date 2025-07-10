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
    public class CholesterolProcessorTests : IDisposable
    {
        protected readonly Mock<IGeneratorService> _generatorMock;
        protected readonly Mock<IKafkaService> _kafkaServiceMock;
        protected readonly Mock<ILogger<CholesterolProcessor>> _loggerMock;
        protected readonly CholesterolProcessor _processor;
        private readonly IOptions<MetricGenerationConfig> _config;

        public CholesterolProcessorTests()
        {
            _generatorMock = new Mock<IGeneratorService>();
            _kafkaServiceMock = new Mock<IKafkaService>();
            _loggerMock = new Mock<ILogger<CholesterolProcessor>>();

            _config = Options.Create(new MetricGenerationConfig
            {
                CholesterolIntervalSeconds = 300 
            });

            _processor = new CholesterolProcessor(
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
                new CholesterolProcessor(
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
                Cholesterol = new Metric { Value = 5.2 }
            };

            // Генерируем значение в пределах ожидаемого диапазона (без аномалий)
            _generatorMock
                .Setup(g => g.GenerateCholesterol(It.IsAny<double>()))
                .Returns((double prev) =>
                {
                    var baseValue = prev + new Random().NextDouble() * 0.2 - 0.1;
                    return Math.Round(Math.Clamp(baseValue, 3.5, 6.2), 1); // Нормальный диапазон
                });

            var result = await _processor.GenerateMetricValue(patient);

            Assert.InRange(result, 3.5, 6.2); // Проверяем попадание в нормальный диапазон
            _generatorMock.Verify(
                g => g.GenerateCholesterol(patient.Cholesterol.Value),
                Times.Once);
        }

        [Fact]
        public async Task GenerateMetricValue_ReturnsAnomaly_WhenConditionMet()
        {
            var patient = new Patient
            {
                Cholesterol = new Metric { Value = 5.2 }
            };

            // Форсируем аномальное значение
            _generatorMock
                .Setup(g => g.GenerateCholesterol(It.IsAny<double>()))
                .Returns(6.7); // Значение вне нормального диапазона

            var result = await _processor.GenerateMetricValue(patient);

            Assert.InRange(result, 6.5, 10.0); // Аномальный диапазон
            _generatorMock.Verify(
                g => g.GenerateCholesterol(patient.Cholesterol.Value),
                Times.Once);
        }

        [Fact]
        public void UpdatePatientMetric_UpdatesCorrectProperties()
        {
            var patient = new Patient
            {
                Cholesterol = new Metric { Value = 5.0, LastUpdate = DateTime.UtcNow.AddSeconds(-10) }
            };
            const double newValue = 5.3;
            var beforeUpdate = DateTime.UtcNow;

            _processor.UpdatePatientMetric(patient, newValue);
            var afterUpdate = DateTime.UtcNow;

            Assert.Equal(newValue, patient.Cholesterol.Value);
            Assert.True(patient.Cholesterol.LastUpdate >= beforeUpdate);
            Assert.True(patient.Cholesterol.LastUpdate <= afterUpdate);
        }

        [Fact]
        public async Task Update_WhenIntervalPassed_UpdatesPatient()
        {
            const double initialCholesterol = 5.2;
            const double newCholesterol = 5.5;
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Cholesterol = new Metric
                {
                    Value = initialCholesterol,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-125)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "Cholesterol", DateTime.UtcNow.AddSeconds(-125) }
                }
            };

            _generatorMock
                .Setup(g => g.GenerateCholesterol(initialCholesterol))
                .Returns(newCholesterol);

            _kafkaServiceMock
                .Setup(k => k.ProduceAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(newCholesterol, patient.Cholesterol.Value);
            Assert.True((DateTime.UtcNow - patient.Cholesterol.LastUpdate).TotalSeconds < 1);
            _kafkaServiceMock.Verify(
                k => k.ProduceAsync("patient_metrics", It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_WhenIntervalNotPassed_SkipsPatient()
        {
            const double initialCholesterol = 5.2;
            var patient = new Patient
            {
                Cholesterol = new Metric
                {
                    Value = initialCholesterol,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-60)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "Cholesterol", DateTime.UtcNow.AddSeconds(-60) }
                }
            };

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(initialCholesterol, patient.Cholesterol.Value);
            _generatorMock.Verify(
                g => g.GenerateCholesterol(It.IsAny<double>()),
                Times.Never);
        }

        [Fact]
        public async Task Update_WhenException_LogsError()
        {
            var patients = new List<Patient> { new Patient() };
            var expectedException = new InvalidOperationException("Test error");

            _generatorMock
                .Setup(g => g.GenerateCholesterol(It.IsAny<double>()))
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
            const double expectedCholesterol = 5.1;
            var patient = new Patient
            {
                Cholesterol = new Metric { Value = expectedCholesterol }
            };

            var result = _processor.GetMetricValue(patient);
            Assert.Equal(expectedCholesterol, result);
        }

        [Fact]
        public void GetUnit_ReturnsMmolPerLiter()
        {
            var unit = _processor.GetUnit();
            Assert.Equal("ммоль/л", unit);
        }

        [Fact]
        public void GetMetricType_ReturnsCholesterol()
        {
            var metricType = _processor.GetMetricType();
            Assert.Equal(MetricType.Cholesterol, metricType);
        }
    }
}