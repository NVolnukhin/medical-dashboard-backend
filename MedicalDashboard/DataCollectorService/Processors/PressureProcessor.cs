using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;

namespace DataCollectorService.Processors
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
                var systolic = _generator.GenerateSystolicPressure();
                var diastolic = _generator.GenerateDiastolicPressure();
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
