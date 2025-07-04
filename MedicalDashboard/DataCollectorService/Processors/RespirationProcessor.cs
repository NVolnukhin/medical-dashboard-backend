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
    public class RespirationProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;

        public RespirationProcessor(IGeneratorService generator, IOptions<MetricGenerationConfig> intervalSeconds)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
        }

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["Respiration"] >= _intervalSeconds.RespirationIntervalSeconds)
            {
                patient.Respiration.Value = _generator.GenerateRespiration(patient.Respiration.Value);
                patient.Respiration.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Respiration"] = 0;
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Частота дыхания: {patient.Respiration.Value} вдохов/мин");
        }
    }
}
