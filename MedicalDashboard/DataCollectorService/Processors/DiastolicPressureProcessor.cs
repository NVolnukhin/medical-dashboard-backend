using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;
using Shared;

namespace DataCollectorService.Processors
{
    public class DiastolicPressureProcessor : MetricProcessorBase
    {
        private readonly MetricGenerationConfig _config;

        public DiastolicPressureProcessor(IGeneratorService generator,
            IKafkaService kafkaService,
            IOptions<MetricGenerationConfig> config,
            ILogger<DiastolicPressureProcessor> logger)
            : base(generator, kafkaService, logger)
        {
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        }

        protected override MetricType GetMetricType() => MetricType.DiastolicPressure;
        protected override int GetIntervalSeconds() => _config.PressureIntervalSeconds;
        protected override async Task<double> GenerateMetricValue(Patient patient)
        {
            return await Task.FromResult(_generator.GenerateDiastolicPressure());
        }

        protected override void UpdatePatientMetric(Patient patient, double value)
        {
            patient.DiastolicPressure.Value = value;
            patient.DiastolicPressure.LastUpdate = DateTime.UtcNow;
        }

        protected override double GetMetricValue(Patient patient) => patient.DiastolicPressure.Value;
        protected override string GetUnit() => "мм рт.ст.";
    }
}
