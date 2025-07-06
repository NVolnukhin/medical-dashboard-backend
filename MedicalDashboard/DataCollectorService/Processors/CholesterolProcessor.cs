using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Models;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kafka;

namespace Processors
{
    public class CholesterolProcessor : IMetricProcessor
    {
        private readonly IGeneratorService _generator;
        private readonly MetricGenerationConfig _intervalSeconds;
        private readonly IKafkaService _kafkaService;

        public CholesterolProcessor(IGeneratorService generator, 
            IOptions<MetricGenerationConfig> intervalSeconds,
            IKafkaService kafkaService)
        {
            _generator = generator;
            _intervalSeconds = intervalSeconds.Value;
            _kafkaService = kafkaService;
        }

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["Cholesterol"] >= _intervalSeconds.CholesterolIntervalSeconds)
            {
                var newValue = _generator.GenerateCholesterol(patient.Cholesterol.Value);
                patient.Cholesterol.Value = newValue;
                patient.Cholesterol.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Cholesterol"] = 0;

                _kafkaService.SendToKafka(patient, "Cholesterol", newValue);
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Холестерин: {patient.Cholesterol.Value} ммоль/л");
        }
    }
}
