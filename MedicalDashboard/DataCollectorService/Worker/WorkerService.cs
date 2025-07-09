using DataCollectorService.DCSAppContext;
using DataCollectorService.Models;
using DataCollectorService.Observerer;
using DataCollectorService.Processors;
using DataCollectorService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared;
using System;
using System.Data;

namespace DataCollectorService.Worker
{
    public class WorkerService : BackgroundService, ISubject
    {
        private readonly IGeneratorService _generator;
        private readonly List<IObserver> _observers = new();
        private readonly ILogger<WorkerService> _logger;
        private readonly DataCollectorDbContext _dbContext;
        private readonly List<Patient> _patients = new();
        private readonly MetricGenerationConfig _config;
        private readonly List<IMetricProcessor> _metricProcessors = new();
        //public const int BaseInterval = 30;

        public WorkerService(
            IGeneratorService generator,
            ILogger<WorkerService> logger,
            IOptions<MetricGenerationConfig> config,
            IServiceProvider serviceProvider,
            DataCollectorDbContext context,
            IEnumerable<IObserver> observers            )
        {
            _generator = generator;
            _logger = logger;
            _config = config.Value;
            _dbContext = context;

            _metricProcessors = serviceProvider.GetServices<IMetricProcessor>().ToList();
            foreach (var observer in observers)
            {
                Attach(observer);
            }

            _patients = InitPatients();
        }

        private List<Patient> InitPatients()
        {
            var patients = new List<Patient>();
            var dtos = _dbContext.Patients
                .ToList();

            foreach (var dto in dtos)
            {
                var patient = new Patient
                {
                    Id = dto.PatientId,
                    Age = CalculateAge(dto.BirthDate),
                    Height = dto.Height,
                    Name = $"{dto.FirstName} {dto.MiddleName} {dto.LastName}".Trim(),
                    Sex = dto.Sex.ToString(),
                    Ward = dto.Ward
                };

                var metricsDto = _dbContext.Metrics.ToList();
                var weightMetric = metricsDto.FirstOrDefault(m => m.Type == "Weight");
                if (weightMetric != null)
                {
                    patient.BaseWeight = weightMetric.Value;
                }

                patient.InitializeIntervals();
                patients.Add(patient);
            }
            return patients;
        }

        private static int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }

        public void Attach(IObserver observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }

        public void Detach(IObserver observer)
        {
            _observers.Remove(observer);
        }


        private void ParseDataFromDTO(PatientDto patientDto, MetricDto metricDto, Patient patient)
        {
            patient.Age = DateTime.Now.Year - patientDto.BirthDate.Year;
            patient.Height = patientDto.Height;
            patient.Name = patientDto.FirstName + " " + patientDto.MiddleName + " " + patientDto.LastName;
            
            if (metricDto.Type == "Weight")
            {
                patient.BaseWeight = metricDto.Value;
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
                    //_logger.LogInformation($"ПАЦИЕНТЫ      {patientsSnapshot.Count}");
                    await Notify(patientsSnapshot); // Уведомляем наблюдателей о всех пациентах
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в цикле генерации");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }

        public async Task Notify(List<Patient> patients)
        {
            var tasks = new List<Task>();
            foreach (var observer in _observers)
            {
                tasks.Add(observer.Update(patients));
            }
            await Task.WhenAll(tasks);
        }
    }
}
