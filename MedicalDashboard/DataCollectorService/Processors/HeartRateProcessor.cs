using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Kafka;
using Models;
using Services;
using Confluent.Kafka;
using System.Text.Json;

namespace Processors
{
    public class HeartRateProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _config;
        private readonly IKafkaService _kafkaService;

        public HeartRateProcessor(IGeneratorService generator, 
            IOptions<MetricGenerationConfig> config, 
            IKafkaService kafkaService)
        {
            _generator = generator;
            _config = config.Value;
            _kafkaService = kafkaService;
        }

        

        public async void Generate(Patient patient)
        {
            if (patient.MetricIntervals["HeartRate"] >= _config.HeartRateIntervalSeconds)
            {
                var newValue = _generator.GenerateHeartRate(patient.HeartRate.Value);
                patient.HeartRate.Value = newValue;
                patient.HeartRate.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["HeartRate"] = 0;

                await _kafkaService.SendToKafka(patient, "HeartRate", newValue);
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Пульс: {patient.HeartRate.Value} уд/мин");
        }
    }
}
