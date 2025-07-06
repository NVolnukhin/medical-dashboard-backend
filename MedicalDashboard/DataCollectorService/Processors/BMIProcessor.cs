using Microsoft.Extensions.Logging;
using Models;
using Microsoft.Extensions.Options;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kafka;

namespace Processors
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

        public async void Generate(Patient patient)
        {
            if (patient.MetricIntervals["BMI"] >= _intervalSeconds.BmiIntervalSeconds)
            {
                var newValue = _generator.GenerateBMI(patient.BMI.Value, patient.BaseWeight, patient.Height);
                patient.BMI.Value = newValue;
                patient.BMI.LastUpdate = DateTime.UtcNow;
                patient.MetricIntervals["BMI"] = 0;

                await _kafkaService.SendToKafka(patient, "BMI", newValue);
            }
        }


        public void Log(Patient patient, ILogger logger)
        {
            logger.LogInformation($"[{patient.Name}] Индекс массы тела: {Math.Round(patient.BMI.Value, 2)}");
        }
    }
}
