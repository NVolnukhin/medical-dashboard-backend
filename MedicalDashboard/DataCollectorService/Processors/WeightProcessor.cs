using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Processors;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;
using Shared;
using System.Reflection.Emit;

public class WeightProcessor : MetricProcessorBase
{
    private readonly MetricGenerationConfig _config;

    public WeightProcessor(IGeneratorService generator, 
        IKafkaService kafkaService,
        IOptions<MetricGenerationConfig> config, 
        ILogger<WeightProcessor> logger)
        : base(generator, kafkaService, logger)
    {
        _config = config.Value ?? throw new ArgumentNullException(nameof(config));
    }

    protected override MetricType GetMetricType() => MetricType.Weight; // тип метрики
    protected override int GetIntervalSeconds() => _config.WeightIntervalSeconds; // интервал из конфига
    protected override async Task<double> GenerateMetricValue(Patient patient)
    {
        return await Task.FromResult(_generator.GenerateWeight(patient.Weight.Value, patient.BaseWeight)); // генерация 
    }

    protected override void UpdatePatientMetric(Patient patient, double value)
    {
        patient.Weight.Value = value; // обновление значения
        patient.Weight.LastUpdate = DateTime.UtcNow; // рбновление времени
        //_logger.LogInformation($"ВЕС ПАЦИЕНТА {patient.Weight.Value}");
    }

    protected override double GetMetricValue(Patient patient) => patient.Weight.Value; //  текущее значение
    protected override string GetUnit() => "кг"; // ед. измер
}