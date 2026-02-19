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

        public override MetricType GetMetricType() => MetricType.SystolicPressure;
        public override int GetIntervalSeconds() => _config.PressureIntervalSeconds;
        public override async Task<double> GenerateMetricValue(Patient patient)
        {
            return await Task.FromResult(_generator.GenerateSystolicPressure());
        }

        public override void UpdatePatientMetric(Patient patient, double value)
        {
            patient.SystolicPressure.Value = value;
            patient.SystolicPressure.LastUpdate = DateTime.UtcNow;
        }

        public override double GetMetricValue(Patient patient) => patient.SystolicPressure.Value;
        public override string GetUnit() => "мм рт.ст.";
    }
}
