using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;
using Shared;
using System.Reflection.Emit;

namespace DataCollectorService.Processors
{
    public class SystolicPressureProcessor : MetricProcessorBase
    {
        private readonly MetricGenerationConfig _config;

        public SystolicPressureProcessor(IGeneratorService generator,
            IKafkaService kafkaService,
            IOptions<MetricGenerationConfig> config,
            ILogger<SystolicPressureProcessor> logger)
            : base(generator, kafkaService, logger)
        {
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        }

        protected override MetricType GetMetricType() => MetricType.SystolicPressure;
        protected override int GetIntervalSeconds() => _config.PressureIntervalSeconds;
        protected override async Task<double> GenerateMetricValue(Patient patient)
        {
            return await Task.FromResult(_generator.GenerateSystolicPressure());
        }

        protected override void UpdatePatientMetric(Patient patient, double value)
        {
            patient.SystolicPressure.Value = value;
            patient.SystolicPressure.LastUpdate = DateTime.UtcNow;
        }

        protected override double GetMetricValue(Patient patient) => patient.SystolicPressure.Value;
        protected override string GetUnit() => "мм рт.ст.";
    }
}
