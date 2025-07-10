using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;
using Shared;
using System.Reflection.Emit;

namespace DataCollectorService.Processors
{
    public class RespirationProcessor : MetricProcessorBase
    {
        private readonly MetricGenerationConfig _config;

        public RespirationProcessor(IGeneratorService generator,
            IKafkaService kafkaService,
            IOptions<MetricGenerationConfig> config,
            ILogger<RespirationProcessor> logger)
            : base(generator, kafkaService, logger)
        {
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        }

        public override MetricType GetMetricType() => MetricType.RespirationRate;
        public override int GetIntervalSeconds() => _config.RespirationIntervalSeconds;
        public override async Task<double> GenerateMetricValue(Patient patient)
        {
            return await Task.FromResult(_generator.GenerateRespiration(patient.RespirationRate.Value));
        }

        public override void UpdatePatientMetric(Patient patient, double value)
        {
            patient.RespirationRate.Value = value;
            patient.RespirationRate.LastUpdate = DateTime.UtcNow;
        }

        public override double GetMetricValue(Patient patient) => patient.RespirationRate.Value;
        public override string GetUnit() => "вдохов/мин";
    }
}
