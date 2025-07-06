using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;

namespace DataCollectorService.Processors
{
    public class SaturationProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;
        private readonly IKafkaService _kafkaService;

        public SaturationProcessor(IGeneratorService generator, 
            IOptions<MetricGenerationConfig> intervalSeconds,
            IKafkaService kafkaService)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
            _kafkaService = kafkaService;
        }

        public async Task Generate(Patient patient)
        {
            if (patient.MetricIntervals["Saturation"] >= _intervalSeconds.SaturationIntervalSeconds)
            {
                var newValue = _generator.GenerateSaturation(patient.Saturation.Value);
                patient.Saturation.Value = newValue;
                patient.Saturation.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Saturation"] = 0;

                await _kafkaService.SendToAllTopics(patient, "Saturation", newValue);
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Сатурация: {patient.Saturation.Value}%");
        }
    }
}
