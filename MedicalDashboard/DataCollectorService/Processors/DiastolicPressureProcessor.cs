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

        public override MetricType GetMetricType() => MetricType.DiastolicPressure;
        public override int GetIntervalSeconds() => _config.PressureIntervalSeconds;
        public override async Task<double> GenerateMetricValue(Patient patient)
        {
            return await Task.FromResult(_generator.GenerateDiastolicPressure());
        }

        public override void UpdatePatientMetric(Patient patient, double value)
        {
            patient.DiastolicPressure.Value = value;
            patient.DiastolicPressure.LastUpdate = DateTime.UtcNow;
        }

        public override double GetMetricValue(Patient patient) => patient.DiastolicPressure.Value;
        public override string GetUnit() => "мм рт.ст.";
    }
}
