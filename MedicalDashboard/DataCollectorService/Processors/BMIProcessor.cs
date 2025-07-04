using Microsoft.Extensions.Logging;
using Models;
using Microsoft.Extensions.Options;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Processors
{
    public class BMIProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;

        public BMIProcessor(IGeneratorService generator, IOptions<MetricGenerationConfig> intervalSeconds)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
        }

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["BMI"] >= _intervalSeconds.BmiIntervalSeconds)
            {
                patient.BMI.Value = _generator.GenerateBMI(patient.BMI.Value, patient.BaseWeight, patient.Height);
                patient.BMI.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["BMI"] = 0;
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Индекс массы тела: {Math.Round(patient.BMI.Value, 2)}");
        }
    }
}
