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
    public class HemoglobinProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;

        public HemoglobinProcessor(IGeneratorService generator, IOptions<MetricGenerationConfig> intervalSeconds)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
        }

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["Hemoglobin"] >= _intervalSeconds.HemoglobinIntervalSeconds)
            {
                patient.Hemoglobin.Value = _generator.GenerateHemoglobin(patient.Hemoglobin.Value);
                patient.Hemoglobin.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Hemoglobin"] = 0;
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Гемоглобин: {patient.Hemoglobin.Value}");
        }
    }
}
