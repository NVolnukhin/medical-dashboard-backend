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

        protected override MetricType GetMetricType() => MetricType.RespirationRate;
        protected override int GetIntervalSeconds() => _config.RespirationIntervalSeconds;
        protected override async Task<double> GenerateMetricValue(Patient patient)
        {
            return await Task.FromResult(_generator.GenerateRespiration(patient.RespirationRate.Value));
        }

        protected override void UpdatePatientMetric(Patient patient, double value)
        {
            patient.RespirationRate.Value = value;
            patient.RespirationRate.LastUpdate = DateTime.UtcNow;
        }

        protected override double GetMetricValue(Patient patient) => patient.RespirationRate.Value;
        protected override string GetUnit() => "вдохов/мин";
    }
}
