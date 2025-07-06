using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Services;
using Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Processors
{
    public class HemoglobinProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;
        private readonly IKafkaService _kafkaService;

        public HemoglobinProcessor(IGeneratorService generator, 
            IOptions<MetricGenerationConfig> intervalSeconds,
            IKafkaService kafkaService)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
            _kafkaService = kafkaService;
        }

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["Hemoglobin"] >= _intervalSeconds.HemoglobinIntervalSeconds)
            {
                var newValue = _generator.GenerateHemoglobin(patient.Hemoglobin.Value);
                patient.Hemoglobin.Value = newValue;
                patient.Hemoglobin.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Hemoglobin"] = 0;

                _kafkaService.SendToKafka(patient, "Hemoglobin", newValue);
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Гемоглобин: {patient.Hemoglobin.Value}");
        }
    }
}
