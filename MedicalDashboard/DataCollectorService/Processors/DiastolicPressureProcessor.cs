using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;

namespace DataCollectorService.Processors
{
    public class DiastolicPressureProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;
        private readonly IKafkaService _kafkaService;

        public DiastolicPressureProcessor(IGeneratorService generator,
            IOptions<MetricGenerationConfig> intervalSeconds,
            IKafkaService kafkaService)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
            _kafkaService = kafkaService;
        }

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["DiasPressure"] >= _intervalSeconds.PressureIntervalSeconds)
            {
                var diastolic = _generator.GenerateDiastolicPressure();
                patient.DiasPressure.Value = diastolic;
                patient.DiasPressure.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["DiasPressure"] = 0;

                _kafkaService.SendToKafka(patient, "DiasPressure", diastolic);
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Диастолическое давление: " +
                $"{patient.DiasPressure.Value} мм рт.ст.");
        }
    }
}
