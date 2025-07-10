using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;
using Shared;
using System.Reflection.Emit;

namespace DataCollectorService.Processors
{
    public class BMIProcessor : MetricProcessorBase
    {
        private readonly MetricGenerationConfig _config;

        public BMIProcessor(IGeneratorService generator,
            IKafkaService kafkaService,
            IOptions<MetricGenerationConfig> config,
            ILogger<BMIProcessor> logger)
            : base(generator, kafkaService, logger)
        {
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        }

        public override MetricType GetMetricType() => MetricType.BMI;
        public override int GetIntervalSeconds() => _config.BmiIntervalSeconds;
        public override async Task<double> GenerateMetricValue(Patient patient)
        {
            return await Task.FromResult(_generator.GenerateBMI(patient.BMI.Value, patient.BaseWeight, patient.Height));
        }

        public override void UpdatePatientMetric(Patient patient, double value)
        {
            patient.BMI.Value = value;
            patient.BMI.LastUpdate = DateTime.UtcNow;
            //_logger.LogInformation($"ВЕС ПАЦИЕНТА {patient.Weight.Value}");
        }

        public override double GetMetricValue(Patient patient) => patient.BMI.Value;
        public override string GetUnit() => "кг/м2";
    }
}
