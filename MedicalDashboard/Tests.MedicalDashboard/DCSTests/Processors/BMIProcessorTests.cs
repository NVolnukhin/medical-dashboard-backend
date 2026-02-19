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
    public class BMIProcessorTests : IDisposable
    {
        protected readonly Mock<IGeneratorService> _generatorMock;
        protected readonly Mock<IKafkaService> _kafkaServiceMock;
        protected readonly Mock<ILogger<BMIProcessor>> _loggerMock;
        protected readonly BMIProcessor _processor;
        private readonly IOptions<MetricGenerationConfig> _config;

        public BMIProcessorTests()
        {
            _generatorMock = new Mock<IGeneratorService>();
            _kafkaServiceMock = new Mock<IKafkaService>();
            _loggerMock = new Mock<ILogger<BMIProcessor>>();

            _config = Options.Create(new MetricGenerationConfig
            {
                BmiIntervalSeconds = 30 
            });

            _processor = new BMIProcessor(
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
                new BMIProcessor(
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
                BMI = new Metric { Value = 24.5 },
                BaseWeight = 70.0,
                Height = 1.75
            };
            const double expectedBmi = 24.6;

            _generatorMock
                .Setup(g => g.GenerateBMI(patient.BMI.Value, patient.BaseWeight, patient.Height))
                .Returns(expectedBmi);

            var result = await _processor.GenerateMetricValue(patient);

            Assert.Equal(expectedBmi, result);
            _generatorMock.Verify(
                g => g.GenerateBMI(patient.BMI.Value, patient.BaseWeight, patient.Height),
                Times.Once);
        }

        [Fact]
        public void UpdatePatientMetric_UpdatesCorrectProperties()
        {
            var patient = new Patient
            {
                BMI = new Metric { Value = 24.5, LastUpdate = DateTime.UtcNow.AddSeconds(-10) },
                BaseWeight = 70.0,
                Height = 1.75
            };
            const double newValue = 24.7;
            var beforeUpdate = DateTime.UtcNow;

            _processor.UpdatePatientMetric(patient, newValue);
            var afterUpdate = DateTime.UtcNow;

            Assert.Equal(newValue, patient.BMI.Value);
            Assert.True(patient.BMI.LastUpdate >= beforeUpdate);
            Assert.True(patient.BMI.LastUpdate <= afterUpdate);
        }

        [Fact]
        public async Task Update_IntervalPassed_PatientProcessed()
        {
            var now = DateTime.UtcNow;
            var id = Guid.NewGuid();
            var patient = new Patient
            
            {
                Id = id,
                Name = "Test Patient",
                Height = 180, 
                BaseWeight = 70,
                BMI = new Metric { Value = 24.5 },
                MetricLastGenerations = new Dictionary<string, DateTime>
        {
            { "BMI", now.AddSeconds(-31) } // Интервал 30 сек, прошло 31 (> интервала)
        }
            };
            var patients = new List<Patient> { patient };
            const double expectedBmi = 25.0;
            _generatorMock
                .Setup(x => x.GenerateBMI(24.5, patient.BaseWeight, patient.Height))
                .Returns(expectedBmi);

            await _processor.Update(patients);
            
            _generatorMock.Verify(
                x => x.GenerateBMI(24.5, patient.BaseWeight, patient.Height),
                Times.Once);
            Assert.Equal(expectedBmi, patient.BMI.Value); 
            Assert.Equal(now, patient.MetricLastGenerations["BMI"], TimeSpan.FromMilliseconds(100)); // Допуск 100 мс

            _kafkaServiceMock.Verify(
                x => x.SendToAllTopics(patient, "BMI", expectedBmi),
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
                { "BMI", now.AddSeconds(-25) } // Интервал 30 сек, прошло только 25
            }
        }
    };

            await _processor.Update(patients);

            _generatorMock.Verify(
                x => x.GenerateWeight(It.IsAny<double?>(), It.IsAny<double>()),
                Times.Never);
            _kafkaServiceMock.Verify(
                x => x.SendToAllTopics(It.IsAny<Patient>(), It.IsAny<string>(), It.IsAny<double>()),
                Times.Never);
            _loggerMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_WhenPatientsIsNull_LogsError()
        {
            List<Patient> nullPatients = null!;
            var expectedMessage = "Ошибка в Update для BMI";

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
            const double expectedBmi = 25.0;
            var patient = new Patient
            {
                BMI = new Metric { Value = expectedBmi }
            };

            var result = _processor.GetMetricValue(patient);
            Assert.Equal(expectedBmi, result);
        }

        [Fact]
        public void GetUnit_ReturnsKgPerM2()
        {
            var unit = _processor.GetUnit();
            Assert.Equal("кг/м2", unit);
        }

        [Fact]
        public void GetMetricType_ReturnsBmi()
        {
            var metricType = _processor.GetMetricType();
            Assert.Equal(MetricType.BMI, metricType);
        }
    }
}