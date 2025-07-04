using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Models;
using Processors;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public class WorkerService : BackgroundService
    {
        private readonly IGeneratorService _generator;
        private readonly ILogger<WorkerService> _logger;
        private readonly List<Patient> _patients = new();
        private readonly MetricGenerationConfig _config;
        private readonly List<IMetricProcessor> _metricProcessors = new();
        public const int BaseInterval = 30;

        public WorkerService(
            IGeneratorService generator,
            ILogger<WorkerService> logger,
            IOptions<MetricGenerationConfig> config,
            IServiceProvider serviceProvider)
        {
            _generator = generator;
            _logger = logger;
            _config = config.Value;

            _metricProcessors = serviceProvider.GetServices<IMetricProcessor>().ToList();

            InitPatients();

            //var cfg = config.Value;
            //_metricProcessors.Add(new HeartRateProcessor(generator, cfg.HeartRateIntervalSeconds));
            //_metricProcessors.Add(new SaturationProcessor(generator, cfg.SaturationIntervalSeconds));
            //_metricProcessors.Add(new TemperatureProcessor(generator, cfg.TemperatureIntervalSeconds));
            //_metricProcessors.Add(new RespirationProcessor(generator, cfg.RespirationIntervalSeconds));
            //_metricProcessors.Add(new PressureProcessor(generator, cfg.PressureIntervalSeconds));
            //_metricProcessors.Add(new HemoglobinProcessor(generator, cfg.HemoglobinIntervalSeconds));
            //_metricProcessors.Add(new WeightProcessor(generator, cfg.WeightIntervalSeconds));
            //_metricProcessors.Add(new CholesterolProcessor(generator, cfg.CholesterolIntervalSeconds));
        }

        private void InitPatients()
        {
            // Временно добавила инициализацию отдельных пациентов
            _patients.Add(new Patient { Name = "Петрова Анна Михайловна", BaseWeight = 60.0, Height = 1.52 });
            _patients.Add(new Patient { Name = "Фролова Ольга Анатольевна", BaseWeight = 78.0, Height = 1.84 });

            foreach (var patient in _patients) 
            {
                patient.InitializeIntervals();
            }
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("Сервис генерации данных запущен");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var patientsSnapshot = _patients.ToList();

                    foreach (var patient in patientsSnapshot)
                    {
                        foreach (var metric in patient.MetricIntervals.Keys.ToList())
                        {
                            patient.MetricIntervals[metric] += BaseInterval;
                        }

                        foreach (var processor in _metricProcessors) 
                        {
                            processor.Generate(patient);
                            processor.Log(patient, _logger);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в цикле генерации");
                }

                await Task.Delay(TimeSpan.FromSeconds(BaseInterval), ct);
            }
        }
    }
}
