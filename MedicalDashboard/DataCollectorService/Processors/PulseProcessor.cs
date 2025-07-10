using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;
using Shared;

namespace DataCollectorService.Processors
{
    public class PulseProcessor : MetricProcessorBase
    {
        private readonly MetricGenerationConfig _config;

        public PulseProcessor(IGeneratorService generator,
            IKafkaService kafkaService,
            IOptions<MetricGenerationConfig> config,
            ILogger<PulseProcessor> logger)
            : base(generator, kafkaService, logger)
        {
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        }

        public override MetricType GetMetricType() => MetricType.Pulse;
        public override int GetIntervalSeconds() => _config.PulseIntervalSeconds;
        public override async Task<double> GenerateMetricValue(Patient patient)
        {
            return await Task.FromResult(_generator.GeneratePulse(patient.Pulse.Value));
        }

        public override void UpdatePatientMetric(Patient patient, double value)
        {
            patient.Pulse.Value = value;
            patient.Pulse.LastUpdate = DateTime.UtcNow;
        }

        public override double GetMetricValue(Patient patient) => patient.Pulse.Value;
        public override string GetUnit() => "уд./мин";
    }
}
