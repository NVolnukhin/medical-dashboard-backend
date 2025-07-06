using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;

namespace DataCollectorService.Processors
{
    public class PressureProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;
        private readonly IKafkaService _kafkaService;

        public PressureProcessor(IGeneratorService generator, 
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
                var diastolic = _generator.GenerateDiastolicPressure();
                patient.SysPressure.Value = systolic;
                patient.DiasPressure.Value = diastolic;
                patient.SysPressure.LastUpdate = DateTime.UtcNow;
                patient.DiasPressure.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["SysPressure"] = 0;
                patient.MetricIntervals["DiasPressure"] = 0;

                await _kafkaService.SendToAllTopics(patient, "SysPressure", systolic);
                await _kafkaService.SendToAllTopics(patient, "DiasPressure", diastolic);
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Давление: " +
                $"{patient.SysPressure.Value}/{patient.DiasPressure.Value} мм рт.ст.");
        }
    }
}
