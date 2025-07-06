using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;

namespace DataCollectorService.Processors
{
    public class TemperatureProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;
        private readonly IKafkaService _kafkaService;

        public TemperatureProcessor(IGeneratorService generator, 
            IOptions<MetricGenerationConfig> intervalSeconds,
            IKafkaService kafkaService)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
            _kafkaService = kafkaService;
        }

        public async Task Generate(Patient patient)
        {
            if (patient.MetricIntervals["Temperature"] >= _intervalSeconds.TemperatureIntervalSeconds)
            {
                var newValue = _generator.GenerateTemperature(patient.Temperature.Value);
                patient.Temperature.Value = newValue;
                patient.Temperature.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Temperature"] = 0;

                await _kafkaService.SendToAllTopics(patient, "Temperature", newValue);
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Температура: {patient.Temperature.Value}°C");
        }
    }
}
