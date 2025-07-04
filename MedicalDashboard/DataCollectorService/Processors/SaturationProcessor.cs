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
    public class SaturationProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;

        public SaturationProcessor(IGeneratorService generator, IOptions<MetricGenerationConfig> intervalSeconds)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
        }

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["Saturation"] >= _intervalSeconds.SaturationIntervalSeconds)
            {
                patient.Saturation.Value = _generator.GenerateSaturation(patient.Saturation.Value);
                patient.Saturation.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Saturation"] = 0;
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Сатурация: {patient.Saturation.Value}%");
        }
    }
}
