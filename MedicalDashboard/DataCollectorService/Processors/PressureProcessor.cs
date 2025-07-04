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
    public class PressureProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;

        public PressureProcessor(IGeneratorService generator, IOptions<MetricGenerationConfig> intervalSeconds)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
        }

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["Pressure"] >= _intervalSeconds.PressureIntervalSeconds)
            {
                var (systolic, diastolic) = _generator.GeneratePressure();
                patient.SysPressure.Value = systolic;
                patient.DiasPressure.Value = diastolic;
                patient.SysPressure.LastUpdate = DateTime.UtcNow;
                patient.DiasPressure.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Pressure"] = 0;
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Давление: " +
                $"{patient.SysPressure.Value}/{patient.DiasPressure.Value} мм рт.ст.");
        }
    }
}
