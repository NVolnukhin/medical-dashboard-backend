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
    public class CholesterolProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;

        public CholesterolProcessor(IGeneratorService generator, IOptions<MetricGenerationConfig> intervalSeconds)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
        }

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["Cholesterol"] >= _intervalSeconds.CholesterolIntervalSeconds)
            {
                patient.Cholesterol.Value = _generator.GenerateCholesterol(patient.Cholesterol.Value);
                patient.Cholesterol.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Cholesterol"] = 0;
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Холестерин: {patient.Cholesterol.Value} ммоль/л");
        }
    }
}
