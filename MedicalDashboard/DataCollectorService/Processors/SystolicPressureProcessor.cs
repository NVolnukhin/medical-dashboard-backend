using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;

namespace DataCollectorService.Processors
{
    public class SystolicPressureProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;
        private readonly IKafkaService _kafkaService;

        public SystolicPressureProcessor(IGeneratorService generator, 
            IOptions<MetricGenerationConfig> intervalSeconds,
            IKafkaService kafkaService)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
            _kafkaService = kafkaService;
        }

        public async Task Generate(Patient patient)
        {
            if (patient.MetricIntervals["SysPressure"] >= _intervalSeconds.PressureIntervalSeconds)
            {
                var systolic = _generator.GenerateSystolicPressure();
                patient.SysPressure.Value = systolic;
                patient.SysPressure.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["SysPressure"] = 0;

                await _kafkaService.SendToAllTopics(patient, "SysPressure", systolic);
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Систолическое давление: " +
                $"{patient.SysPressure.Value} мм рт.ст.");
        }
    }
}
