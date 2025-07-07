using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;
using Shared;
using System.Reflection.Emit;

namespace DataCollectorService.Processors
{
    public class CholesterolProcessor : MetricProcessorBase
    {
        private readonly MetricGenerationConfig _config;

        public CholesterolProcessor(IGeneratorService generator,
            IKafkaService kafkaService,
            IOptions<MetricGenerationConfig> config,
            ILogger<CholesterolProcessor> logger)
            : base(generator, kafkaService, logger)
        {
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        }

        protected override MetricType GetMetricType() => MetricType.Cholesterol;
        protected override int GetIntervalSeconds() => _config.CholesterolIntervalSeconds;
        protected override async Task<double> GenerateMetricValue(Patient patient)
        {
            return await Task.FromResult(_generator.GenerateCholesterol(patient.Cholesterol.Value));
        }

        protected override void UpdatePatientMetric(Patient patient, double value)
        {
            patient.Cholesterol.Value = value;
            patient.Cholesterol.LastUpdate = DateTime.UtcNow;
        }

        protected override double GetMetricValue(Patient patient) => patient.Cholesterol.Value;
        protected override string GetUnit() => "ммоль/л";
    }
}
