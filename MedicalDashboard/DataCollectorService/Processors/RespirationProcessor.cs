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
    public class RespirationProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;
        private readonly IKafkaService _kafkaService;

        public RespirationProcessor(IGeneratorService generator, 
            IOptions<MetricGenerationConfig> intervalSeconds,
            IKafkaService kafkaService)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
            _kafkaService = kafkaService;
        }

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["Respiration"] >= _intervalSeconds.RespirationIntervalSeconds)
            {
                var newValue = _generator.GenerateRespiration(patient.Respiration.Value);
                patient.Respiration.Value = newValue;
                patient.Respiration.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Respiration"] = 0;

                _kafkaService.SendToKafka(patient, "Respiration", newValue);
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Частота дыхания: {patient.Respiration.Value} вдохов/мин");
        }
    }
}
