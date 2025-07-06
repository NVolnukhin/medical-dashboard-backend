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

        public void Generate(Patient patient)
        {
            if (patient.MetricIntervals["SysPressure"] >= _intervalSeconds.PressureIntervalSeconds)
            {
                var systolic = _generator.GenerateSystolicPressure();
                patient.SysPressure.Value = systolic;
                patient.SysPressure.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["SysPressure"] = 0;

                _kafkaService.SendToKafka(patient, "SysPressure", systolic);
            }
        }

        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Систолическое давление: " +
                $"{patient.SysPressure.Value} мм рт.ст.");
        }
    }
}
