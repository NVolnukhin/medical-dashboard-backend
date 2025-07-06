using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;

namespace DataCollectorService.Processors
{
    public class BMIProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;
        private readonly IKafkaService _kafkaService;

        public BMIProcessor(IGeneratorService generator, 
            IOptions<MetricGenerationConfig> intervalSeconds,
            IKafkaService kafkaService)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
            _kafkaService = kafkaService;
        }

        public async Task Generate(Patient patient)
        {
            if (patient.MetricIntervals["BMI"] >= _intervalSeconds.BmiIntervalSeconds)
            {
                var newValue = _generator.GenerateBMI(patient.BMI.Value, patient.BaseWeight, patient.Height);
                patient.BMI.Value = newValue;
                patient.BMI.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["BMI"] = 0;

                await _kafkaService.SendToAllTopics(patient, "BMI", newValue);
            }
        }


        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Индекс массы тела: {Math.Round(patient.BMI.Value, 2)}");
        }
    }
}
