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
    public class DiastolicPressureProcessorTests : IDisposable
    {
        protected readonly Mock<IGeneratorService> _generatorMock;
        protected readonly Mock<IKafkaService> _kafkaServiceMock;
        protected readonly Mock<ILogger<DiastolicPressureProcessor>> _loggerMock;
        protected readonly DiastolicPressureProcessor _processor;
        private readonly IOptions<MetricGenerationConfig> _config;

        public DiastolicPressureProcessorTests()
        {
            _generatorMock = new Mock<IGeneratorService>();
            _kafkaServiceMock = new Mock<IKafkaService>();
            _loggerMock = new Mock<ILogger<DiastolicPressureProcessor>>();

            _config = Options.Create(new MetricGenerationConfig
            {
                PressureIntervalSeconds = 60 
            });

            _processor = new DiastolicPressureProcessor(
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
                new DiastolicPressureProcessor(
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
            const double expectedPressure = 80.0;

            _generatorMock
                .Setup(g => g.GenerateDiastolicPressure())
                .Returns(expectedPressure);

            var result = await _processor.GenerateMetricValue(patient);

            Assert.Equal(expectedPressure, result);
            _generatorMock.Verify(
                g => g.GenerateDiastolicPressure(),
                Times.Once);
        }

        [Fact]
        public void UpdatePatientMetric_UpdatesCorrectProperties()
        {
            var patient = new Patient
            {
                DiastolicPressure = new Metric { Value = 70.0, LastUpdate = DateTime.UtcNow.AddDays(-1) }
            };
            const double newValue = 75.0;
            var beforeUpdate = DateTime.UtcNow;

            _processor.UpdatePatientMetric(patient, newValue);
            var afterUpdate = DateTime.UtcNow;

            Assert.Equal(newValue, patient.DiastolicPressure.Value);
            Assert.True(patient.DiastolicPressure.LastUpdate >= beforeUpdate);
            Assert.True(patient.DiastolicPressure.LastUpdate <= afterUpdate);
        }

        //[Fact]
        //public async Task Update_WhenIntervalPassed_UpdatesPatient()
        //{
        //    const double initialPressure = 70.0;
        //    const double newPressure = 75.0;
        //    var patient = new Patient
        //    {
        //        Id = Guid.NewGuid(),
        //        DiastolicPressure = new Metric
        //        {
        //            Value = initialPressure,
        //            LastUpdate = DateTime.UtcNow.AddSeconds(-70) // Больше интервала (60s)
        //        },
        //        MetricLastGenerations = new Dictionary<string, DateTime>
        //        {
        //            { "DiastolicPressure", DateTime.UtcNow.AddSeconds(-70) }
        //        }
        //    };

        //    _generatorMock
        //        .Setup(g => g.GenerateDiastolicPressure())
        //        .Returns(newPressure);

        //    _kafkaServiceMock
        //        .Setup(k => k.ProduceAsync(It.IsAny<string>(), It.IsAny<string>()))
        //        .Returns(Task.CompletedTask);

        //    await _processor.Update(new List<Patient> { patient });

        //    Assert.Equal(newPressure, patient.DiastolicPressure.Value);
        //    Assert.True((DateTime.UtcNow - patient.DiastolicPressure.LastUpdate).TotalSeconds < 1);
        //    _kafkaServiceMock.Verify(
        //        k => k.ProduceAsync("patient_metrics", It.IsAny<string>()),
        //        Times.Once);
        //}

        [Fact]
        public async Task Update_WhenIntervalPassed_ProcessesPatient()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var id = Guid.NewGuid();
            var patient = new Patient
            {
                Id = id,
                Name = "Test Patient",
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    ["DiastolicPressure"] = now.AddSeconds(-61) // Интервал 60 секунд - прошло 61
                },
                DiastolicPressure = new Metric { Value = 70 } // Старое значение
            };
            var patients = new List<Patient> { patient };

            double generatedValue = 85;
            _generatorMock.Setup(g => g.GenerateDiastolicPressure()).Returns(generatedValue);

            // Act
            await _processor.Update(patients);

            // Assert
            // Проверка генерации
            _generatorMock.Verify(g => g.GenerateDiastolicPressure(), Times.Once);

            // Проверка обновления пациента
            Assert.Equal(generatedValue, patient.DiastolicPressure.Value);
            Assert.InRange(patient.DiastolicPressure.LastUpdate, now.AddSeconds(-1), now.AddSeconds(1));
            Assert.InRange(patient.MetricLastGenerations["DiastolicPressure"], now.AddSeconds(-1), now.AddSeconds(1));

            // Проверка отправки в Kafka
            _kafkaServiceMock.Verify(
                k => k.SendToAllTopics(patient, "DiastolicPressure", generatedValue),
                Times.Once);

            // Проверка логгирования успеха
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains($"Generated DiastolicPressure for {patient.Name}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_WhenIntervalNotPassed_SkipsPatient()
        {
            const double initialPressure = 70.0;
            var patient = new Patient
            {
                DiastolicPressure = new Metric
                {
                    Value = initialPressure,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-50) // Меньше интервала (60s)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "DiastolicPressure", DateTime.UtcNow.AddSeconds(-50) }
                }
            };

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(initialPressure, patient.DiastolicPressure.Value);
            _generatorMock.Verify(
                g => g.GenerateDiastolicPressure(),
                Times.Never);
        }

        [Fact]
        public async Task Update_WhenPatientsIsNull_LogsError()
        {
            // Arrange
            List<Patient> nullPatients = null!;
            var expectedMessage = "Ошибка в Update для DiastolicPressure";

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
            const double expectedPressure = 80.0;
            var patient = new Patient
            {
                DiastolicPressure = new Metric { Value = expectedPressure }
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
            var patient1 = new Patient { DiastolicPressure = new Metric { Value = 70.0 } };
            var patient2 = new Patient { DiastolicPressure = new Metric { Value = 90.0 } };
            const double fixedPressure = 80.0;

            _generatorMock
                .Setup(g => g.GenerateDiastolicPressure())
                .Returns(fixedPressure);

            var result1 = _processor.GenerateMetricValue(patient1).Result;
            var result2 = _processor.GenerateMetricValue(patient2).Result;

            Assert.Equal(fixedPressure, result1);
            Assert.Equal(fixedPressure, result2);
            _generatorMock.Verify(
                g => g.GenerateDiastolicPressure(),
                Times.Exactly(2));
        }

        [Fact]
        public void GetMetricType_ReturnsDiastolicPressure()
        {
            var metricType = _processor.GetMetricType();

            Assert.Equal(MetricType.DiastolicPressure, metricType);
        }
    }
}