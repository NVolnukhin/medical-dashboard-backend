using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;
using Shared;
using System.Reflection.Emit;

namespace DataCollectorService.Processors
{
    public class TemperatureProcessor : MetricProcessorBase
    {
        private readonly MetricGenerationConfig _config;

        public TemperatureProcessor(IGeneratorService generator,
            IKafkaService kafkaService,
            IOptions<MetricGenerationConfig> config,
            ILogger<TemperatureProcessor> logger)
            : base(generator, kafkaService, logger)
        {
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));
        }

        protected override MetricType GetMetricType() => MetricType.Temperature;
        protected override int GetIntervalSeconds() => _config.TemperatureIntervalSeconds;
        protected override async Task<double> GenerateMetricValue(Patient patient)
        {
            return await Task.FromResult(_generator.GenerateTemperature(patient.Temperature.Value));
        }

        protected override void UpdatePatientMetric(Patient patient, double value)
        {
            patient.Temperature.Value = value;
            patient.Temperature.LastUpdate = DateTime.UtcNow;
        }

        protected override double GetMetricValue(Patient patient) => patient.Temperature.Value; 
        protected override string GetUnit() => "°C"; 
    }
}
