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
    public class WeightProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;

        public WeightProcessor(IGeneratorService generator, IOptions<MetricGenerationConfig> intervalSeconds)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
        }

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["Weight"] >= _intervalSeconds.WeightIntervalSeconds)
            {
                patient.Weight.Value = _generator.GenerateWeight(
                    patient.Weight.Value,
                    patient.BaseWeight);

                patient.Weight.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Weight"] = 0;
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Вес: {patient.Weight.Value} кг");
        }
    }
}
