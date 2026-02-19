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
                    [metricName] = now.AddSeconds(-301) 
                },
                Cholesterol = new Metric { Value = 5.2 } 
            };
            var patients = new List<Patient> { patient };

            double generatedValue = 5.5;
            _generatorMock.Setup(g => g.GenerateCholesterol(patient.Cholesterol.Value)).Returns(generatedValue);

            await _processor.Update(patients);
            _generatorMock.Verify(g => g.GenerateCholesterol(patient.Cholesterol.Value), Times.Once);

            Assert.Equal(generatedValue, patient.Cholesterol.Value);
            Assert.InRange(patient.Cholesterol.LastUpdate, now.AddSeconds(-1), now.AddSeconds(1));
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
        public async Task Update_IntervalNotPassed_PatientSkipped()
        {
            var now = DateTime.UtcNow;
            var patients = new List<Patient>
    {
        new Patient
        {
            MetricLastGenerations = new Dictionary<string, DateTime>
            {
                { "Cholesterol", now.AddSeconds(-250) } // Интервал 300 сек, прошло 250
            }
        }
    };
            await _processor.Update(patients);

            _generatorMock.Verify(
                x => x.GenerateCholesterol(It.IsAny<double?>()),
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
            List<Patient> nullPatients = null!;
            var expectedMessage = "Ошибка в Update для Cholesterol";
            
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