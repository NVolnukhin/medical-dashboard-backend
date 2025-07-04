using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Processors
{
    public class TemperatureProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;

        public TemperatureProcessor(IGeneratorService generator, IOptions<MetricGenerationConfig> intervalSeconds)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
        }

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["Temperature"] >= _intervalSeconds.TemperatureIntervalSeconds)
            {
                patient.Temperature.Value = _generator.GenerateTemperature(patient.Temperature.Value);
                patient.Temperature.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Temperature"] = 0;
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Температура: {patient.Temperature.Value}°C");
        }
    }
}
