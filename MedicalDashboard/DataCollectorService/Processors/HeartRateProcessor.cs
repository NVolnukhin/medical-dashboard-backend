using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Services;

namespace Processors
{
    public class HeartRateProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _config;

        public HeartRateProcessor(IGeneratorService generator, IOptions<MetricGenerationConfig> config)
        {
            _generator = generator;
            _config = config.Value;
        }

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["HeartRate"] >= _config.HeartRateIntervalSeconds)
            {
                patient.HeartRate.Value = _generator.GenerateHeartRate(patient.HeartRate.Value);
                patient.HeartRate.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["HeartRate"] = 0;
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Пульс: {patient.HeartRate.Value} уд/мин");
        }
    }
}
