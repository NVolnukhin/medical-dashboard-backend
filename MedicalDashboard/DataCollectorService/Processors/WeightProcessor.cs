using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;

namespace DataCollectorService.Processors
{
    public class WeightProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;
        private readonly IKafkaService _kafkaService;

        public WeightProcessor(IGeneratorService generator, 
            IOptions<MetricGenerationConfig> intervalSeconds,
            IKafkaService kafkaService)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
            _kafkaService = kafkaService;
        }

        public async Task Generate(Patient patient)
        {
            if (patient.MetricIntervals["Weight"] >= _intervalSeconds.WeightIntervalSeconds)
            {
                var newValue = _generator.GenerateWeight(
                    patient.Weight.Value,
                    patient.BaseWeight);
                patient.Weight.Value = newValue;

                patient.Weight.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Weight"] = 0;

                await _kafkaService.SendToAllTopics(patient, "Weight", newValue);
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Вес: {patient.Weight.Value} кг");
        }
    }
}
