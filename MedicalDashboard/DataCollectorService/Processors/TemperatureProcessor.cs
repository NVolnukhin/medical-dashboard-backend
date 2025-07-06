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

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["Temperature"] >= _intervalSeconds.TemperatureIntervalSeconds)
            {
                var newValue = _generator.GenerateTemperature(patient.Temperature.Value);
                patient.Temperature.Value = newValue;
                patient.Temperature.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["Temperature"] = 0;

                _kafkaService.SendToKafka(patient, "Temperature", newValue);
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Температура: {patient.Temperature.Value}°C");
        }
    }
}
