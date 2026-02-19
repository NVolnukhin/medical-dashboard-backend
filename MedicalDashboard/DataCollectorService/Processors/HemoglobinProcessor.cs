using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;
using Shared;
using System.Reflection.Emit;

namespace DataCollectorService.Processors
{
    public class HemoglobinProcessor : MetricProcessorBase
    {
        private readonly MetricGenerationConfig _config;

        public HemoglobinProcessor(IGeneratorService generator,
            IKafkaService kafkaService,
            IOptions<MetricGenerationConfig> config,
            ILogger<HemoglobinProcessor> logger)
            : base(generator, kafkaService, logger)
        {
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        }

        public override MetricType GetMetricType() => MetricType.Hemoglobin;
        public override int GetIntervalSeconds() => _config.HemoglobinIntervalSeconds;
        public override async Task<double> GenerateMetricValue(Patient patient)
        {
            return await Task.FromResult(_generator.GenerateHemoglobin(patient.Hemoglobin.Value));
        }

        public override void UpdatePatientMetric(Patient patient, double value)
        {
            patient.Hemoglobin.Value = value;
            patient.Hemoglobin.LastUpdate = DateTime.UtcNow;
        }

        public override double GetMetricValue(Patient patient) => patient.Hemoglobin.Value;
        public override string GetUnit() => "г/л";
    }
}
