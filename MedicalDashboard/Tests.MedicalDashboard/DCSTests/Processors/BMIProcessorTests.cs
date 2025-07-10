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
        public async Task Update_WhenIntervalPassed_UpdatesPatient()
        {
            const double initialBmi = 24.5;
            const double newBmi = 24.8;
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                BMI = new Metric
                {
                    Value = initialBmi,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-185)
                },
                BaseWeight = 70.0,
                Height = 1.75,
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "BMI", DateTime.UtcNow.AddSeconds(-185) }
                }
            };

            _generatorMock
                .Setup(g => g.GenerateBMI(initialBmi, patient.BaseWeight, patient.Height))
                .Returns(newBmi);

            _kafkaServiceMock
                .Setup(k => k.ProduceAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(newBmi, patient.BMI.Value);
            Assert.True((DateTime.UtcNow - patient.BMI.LastUpdate).TotalSeconds < 1);
            _kafkaServiceMock.Verify(
                k => k.ProduceAsync("patient_metrics", It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_WhenIntervalNotPassed_SkipsPatient()
        {
            const double initialBmi = 24.5;
            var patient = new Patient
            {
                BMI = new Metric
                {
                    Value = initialBmi,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-90)
                },
                BaseWeight = 70.0,
                Height = 1.75,
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "BMI", DateTime.UtcNow.AddSeconds(-90) }
                }
            };

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(initialBmi, patient.BMI.Value);
            _generatorMock.Verify(
                g => g.GenerateBMI(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()),
                Times.Never);
        }

        [Fact]
        public async Task Update_WhenException_LogsError()
        {
            var patients = new List<Patient> { new Patient() };
            var expectedException = new InvalidOperationException("Test error");

            _generatorMock
                .Setup(g => g.GenerateBMI(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
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