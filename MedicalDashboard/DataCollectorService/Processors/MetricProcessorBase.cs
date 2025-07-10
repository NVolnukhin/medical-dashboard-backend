using DataCollectorService.Kafka;
using DataCollectorService.Models;
using DataCollectorService.Services;
using DataCollectorService.Observerer;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Extensions.Logging;
using Microsoft.Extensions.Logging;

namespace DataCollectorService.Processors;

public abstract class MetricProcessorBase : IMetricProcessor
{
    protected readonly IGeneratorService _generator;
    protected readonly IKafkaService _kafkaService;
    protected readonly ILogger _logger;

    public async Task Update(List<Patient> patients)
    {
        try
        {
            MetricType metric = GetMetricType();
            string metricName = metric.ToString();
            int intervalSeconds = GetIntervalSeconds();

            var now = DateTime.UtcNow;
            var tasks = new List<Task>();


            foreach (var patient in patients)
            {
                if (patient.MetricLastGenerations.TryGetValue(metricName, out DateTime lastGeneration))
                {

                    if ((now - lastGeneration).TotalSeconds >= intervalSeconds)
                    {
                        tasks.Add(ProcessPatientMetric(patient, metricName, now));

                    }
                }
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка в Update для {GetMetricType()}");
        }
    }

    public async Task ProcessPatientMetric(Patient patient, string metricName, DateTime now)
    {
        try
        {
            double newValue = await GenerateMetricValue(patient);
            UpdatePatientMetric(patient, newValue);
            patient.MetricLastGenerations[metricName] = now;

            await _kafkaService.SendToAllTopics(patient, metricName, newValue);
            _logger.LogSuccess($"Generated {metricName} for {patient.Name}: {metricName} = {newValue} {GetUnit()}");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Generate failed for {GetMetricType()}", ex);
        }
    }

    protected abstract MetricType GetMetricType(); //  тип метрики
    protected abstract int GetIntervalSeconds(); //  интервал обновления
    protected abstract Task<double> GenerateMetricValue(Patient patient); // новое значение
    protected abstract void UpdatePatientMetric(Patient patient, double value); // обновить метрику в пациенте
    protected abstract double GetMetricValue(Patient patient); // текущее значение
    protected abstract string GetUnit(); //  ед. измерения

    protected MetricProcessorBase(IGeneratorService generator, IKafkaService kafkaService, ILogger logger)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _kafkaService = kafkaService ?? throw new ArgumentNullException(nameof(kafkaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    //public async Task Generate(Patient patient)
    //{
    //    try
    //    {
    //        MetricType metric = GetMetricType(); // тип метрики
    //        string metricName = metric.ToString();
    //        if (patient.MetricIntervals.ContainsKey(metricName) && patient.MetricIntervals[metricName] >= GetIntervalSeconds())
    //        {
    //            double newValue = await GenerateMetricValue(patient); // ненерируем  значение
    //            UpdatePatientMetric(patient, newValue); // обновляем метрику
    //            await _kafkaService.SendToAllTopics(patient, metricName, newValue);
    //            _logger.LogSuccess($"Generated {metricName} for {patient.Name}: {metricName} = {newValue} {GetUnit()}");
    //            patient.MetricIntervals[metricName] = 0;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogFailure($"Generate failed for {GetMetricType()}", ex);
    //    }


    public void Log(Patient patient, ILogger logger)
    {
        try
        {
            MetricType metric = GetMetricType(); //  тип метрики
            double value = GetMetricValue(patient); // текущее значение
            logger.LogInformation($"[{patient.Name}] {metric}: {value} {GetUnit()}");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Log failed for {GetMetricType()}", ex);
        }
    }
}