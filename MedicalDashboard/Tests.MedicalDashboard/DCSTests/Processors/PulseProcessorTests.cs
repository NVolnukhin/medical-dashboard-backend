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
    public class PulseProcessorTests : IDisposable
    {
        protected readonly Mock<IGeneratorService> _generatorMock;
        protected readonly Mock<IKafkaService> _kafkaServiceMock;
        protected readonly Mock<ILogger<PulseProcessor>> _loggerMock;
        protected readonly PulseProcessor _processor;
        private readonly IOptions<MetricGenerationConfig> _config;

        public PulseProcessorTests()
        {
            _generatorMock = new Mock<IGeneratorService>();
            _kafkaServiceMock = new Mock<IKafkaService>();
            _loggerMock = new Mock<ILogger<PulseProcessor>>();

            _config = Options.Create(new MetricGenerationConfig
            {
                PulseIntervalSeconds = 30 // Интервал для пульса
            });

            _processor = new PulseProcessor(
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
                new PulseProcessor(
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
                Pulse = new Metric { Value = 70.0 }
            };
            const double expectedPulse = 72.5;

            _generatorMock
                .Setup(g => g.GeneratePulse(patient.Pulse.Value))
                .Returns(expectedPulse);

            var result = await _processor.GenerateMetricValue(patient);

            Assert.Equal(expectedPulse, result);
            _generatorMock.Verify(
                g => g.GeneratePulse(patient.Pulse.Value),
                Times.Once);
        }

        [Fact]
        public void UpdatePatientMetric_UpdatesCorrectProperties()
        {
            var patient = new Patient
            {
                Pulse = new Metric { Value = 70.0, LastUpdate = DateTime.UtcNow.AddDays(-1) }
            };
            const double newValue = 72.5;
            var beforeUpdate = DateTime.UtcNow;

            _processor.UpdatePatientMetric(patient, newValue);
            var afterUpdate = DateTime.UtcNow;

            Assert.Equal(newValue, patient.Pulse.Value);
            Assert.True(patient.Pulse.LastUpdate >= beforeUpdate);
            Assert.True(patient.Pulse.LastUpdate <= afterUpdate);
        }

        [Fact]
        public async Task Update_WhenIntervalPassed_UpdatesPatient()
        {
            const double initialPulse = 70.0;
            const double newPulse = 72.5;
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                Pulse = new Metric
                {
                    Value = initialPulse,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-40) // Больше интервала (30s)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "Pulse", DateTime.UtcNow.AddSeconds(-40) }
                }
            };

            _generatorMock
                .Setup(g => g.GeneratePulse(initialPulse))
                .Returns(newPulse);

            _kafkaServiceMock
                .Setup(k => k.ProduceAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(newPulse, patient.Pulse.Value);
            Assert.True((DateTime.UtcNow - patient.Pulse.LastUpdate).TotalSeconds < 1);
            _kafkaServiceMock.Verify(
                k => k.ProduceAsync("patient_metrics", It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Update_WhenIntervalNotPassed_SkipsPatient()
        {
            const double initialPulse = 70.0;
            var patient = new Patient
            {
                Pulse = new Metric
                {
                    Value = initialPulse,
                    LastUpdate = DateTime.UtcNow.AddSeconds(-20) // Меньше интервала (30s)
                },
                MetricLastGenerations = new Dictionary<string, DateTime>
                {
                    { "Pulse", DateTime.UtcNow.AddSeconds(-20) }
                }
            };

            await _processor.Update(new List<Patient> { patient });

            Assert.Equal(initialPulse, patient.Pulse.Value);
            _generatorMock.Verify(
                g => g.GeneratePulse(It.IsAny<double>()),
                Times.Never);
        }

        [Fact]
        public async Task Update_WhenException_LogsError()
        {
            var patients = new List<Patient> { new Patient() };
            var expectedException = new InvalidOperationException("Test error");

            _generatorMock
                .Setup(g => g.GeneratePulse(It.IsAny<double>()))
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
            const double expectedPulse = 68.5;
            var patient = new Patient
            {
                Pulse = new Metric { Value = expectedPulse }
            };

            var result = _processor.GetMetricValue(patient);
            Assert.Equal(expectedPulse, result);
        }

        [Fact]
        public void GetUnit_ReturnsBeatsPerMinute()
        {
            var unit = _processor.GetUnit();
            Assert.Equal("уд./мин", unit);
        }
    }
}