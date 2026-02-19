using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;
using Shared;
using System.Reflection.Emit;

namespace DataCollectorService.Processors
{
    public class SaturationProcessor : MetricProcessorBase
    {
        private readonly MetricGenerationConfig _config;

        public SaturationProcessor(IGeneratorService generator,
            IKafkaService kafkaService,
            IOptions<MetricGenerationConfig> config,
            ILogger<SaturationProcessor> logger)
            : base(generator, kafkaService, logger)
        {
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        }

        public override MetricType GetMetricType() => MetricType.Saturation;
        public override int GetIntervalSeconds() => _config.SaturationIntervalSeconds;
        public override async Task<double> GenerateMetricValue(Patient patient)
        {
            return await Task.FromResult(_generator.GenerateSaturation(patient.Saturation.Value));
        }

        public override void UpdatePatientMetric(Patient patient, double value)
        {
            patient.Saturation.Value = value;
            patient.Saturation.LastUpdate = DateTime.UtcNow;
        }

        public override double GetMetricValue(Patient patient) => patient.Saturation.Value;
        public override string GetUnit() => "%";
    }
}
