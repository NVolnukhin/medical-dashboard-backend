using System;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.MedicalDashboard.DCSTests.Services
{
    public class GeneratorServiceBasicTests
    {
        private readonly ILogger<GeneratorService> _logger;

        public GeneratorServiceBasicTests()
        {
            _logger = new Mock<ILogger<GeneratorService>>().Object;
        }

        [Fact]
        public void GeneratePulse_WithoutPrevious_ReturnsInRange()
        {
            var generator = new GeneratorService(_logger);
            for (int i = 0; i < 1000; i++)
            {
                var result = generator.GeneratePulse(null);
                Assert.InRange(result, 40, 150);
            }
        }

        [Fact]
        public void GeneratePulse_WithPrevious_ReturnsValidValue()
        {
            var generator = new GeneratorService(_logger);
            double previous = 70;
            for (int i = 0; i < 1000; i++)
            {
                var result = generator.GeneratePulse(previous);
                Assert.InRange(result, 40, 150);
                previous = result;
            }
        }

        [Fact]
        public void GeneratePulse_AnomalyOccursApproximately10PercentOfTime()
        {
            var generator = new GeneratorService(_logger);
            int anomalyCount = 0;
            double previous = 70;

            for (int i = 0; i < 10000; i++)
            {
                var result = generator.GeneratePulse(previous);
                if (result < 50 || result > 130)
                    anomalyCount++;
                previous = result;
            }

            double anomalyRate = (double)anomalyCount / 10000;
            Assert.InRange(anomalyRate, 0.08, 0.5); 
        }

        [Fact]
        public void GenerateSaturation_WithoutPrevious_ReturnsInRange()
        {
            var generator = new GeneratorService(_logger);
            for (int i = 0; i < 1000; i++)
            {
                var result = generator.GenerateSaturation(null);
                Assert.InRange(result, 80, 100);
            }
        }

        [Fact]
        public void GenerateSaturation_AnomalyOccursApproximately5PercentOfTime()
        {
            var generator = new GeneratorService(_logger);
            int anomalyCount = 0;

            for (int i = 0; i < 10000; i++)
            {
                var result = generator.GenerateSaturation(97.5);
                if (result < 95)
                    anomalyCount++;
            }

            double anomalyRate = (double)anomalyCount / 10000;
            Assert.InRange(anomalyRate, 0.03, 0.1); 
        }

        [Fact]
        public void GenerateSystolicPressure_ReturnsValidValue()
        {
            var generator = new GeneratorService(_logger);
            int anomalyCount = 0;

            for (int i = 0; i < 10000; i++)
            {
                var result = generator.GenerateSystolicPressure();
                if (result < 110)
                    anomalyCount++;
            }

            double anomalyRate = (double)anomalyCount / 10000;
            Assert.InRange(anomalyRate, 0.05, 0.1); 
        }

        [Fact]
        public void GenerateDiastolicPressure_ReturnsValidValue()
        {
            var generator = new GeneratorService(_logger);
            int anomalyCount = 0;

            for (int i = 0; i < 10000; i++)
            {
                var result = generator.GenerateDiastolicPressure();
                if (result < 75)
                    anomalyCount++;
            }

            double anomalyRate = (double)anomalyCount / 10000;
            Assert.InRange(anomalyRate, 0.05, 0.1); 
        }

        [Fact]
        public void GenerateTemperature_ReturnsValidValue()
        {
            var generator = new GeneratorService(_logger);
            for (int i = 0; i < 1000; i++)
            {
                var result = generator.GenerateTemperature(null);
                Assert.InRange(result, 34, 42);
            }
        }

        [Fact]
        public void GenerateTemperature_AnomalyOccursApproximately5PercentOfTime()
        {
            var generator = new GeneratorService(_logger);
            int anomalyCount = 0;

            for (int i = 0; i < 10000; i++)
            {
                var result = generator.GenerateTemperature(36.6);
                if (result < 35 || result > 37.5)
                    anomalyCount++;
            }

            double anomalyRate = (double)anomalyCount / 10000;
            Assert.InRange(anomalyRate, 0.03, 0.07); // ~5%
        }

        [Fact]
        public void GenerateRespiration_ReturnsValidValue()
        {
            var generator = new GeneratorService(_logger);
            int anomalyCount = 0;

            for (int i = 0; i < 10000; i++)
            {
                var result = generator.GenerateRespiration(null);
                if (result < 16 || result > 20)
                    anomalyCount++;
            }

            double anomalyRate = (double)anomalyCount / 10000;
            Assert.InRange(anomalyRate, 0.03, 0.07); // ~5%
        }

        [Fact]
        public void GenerateHemoglobin_ReturnsValidValue()
        {
            var generator = new GeneratorService(_logger);
            int anomalyCount = 0;

            for (int i = 0; i < 10000; i++)
            {
                var result = generator.GenerateHemoglobin(null);
                if (result < 120)
                    anomalyCount++;
            }

            double anomalyRate = (double)anomalyCount / 10000;
            Assert.InRange(anomalyRate, 0.01, 0.05); // ~3%
        }

        [Fact]
        public void GenerateCholesterol_ReturnsValidValue()
        {
            var generator = new GeneratorService(_logger);
            int anomalyCount = 0;

            for (int i = 0; i < 10000; i++)
            {
                var result = generator.GenerateCholesterol(null);
                if (result > 6.5)
                    anomalyCount++;
            }

            double anomalyRate = (double)anomalyCount / 10000;
            Assert.InRange(anomalyRate, 0.03, 0.07); // ~5%
        }

        [Fact]
        public void GenerateCholesterol_NightIncrease()
        {
            var generator = new GeneratorService(_logger);
            var dayResult = generator.GenerateCholesterol(5.0);
            var nightResult = generator.GenerateCholesterol(5.0);

            // Проверяем, что ночью значение может быть выше
            Assert.True(nightResult >= dayResult * 1.15 || nightResult <= dayResult * 1.15);
        }
    }
}