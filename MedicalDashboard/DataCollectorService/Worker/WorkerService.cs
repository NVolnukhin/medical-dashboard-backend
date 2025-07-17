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
using System.Linq;

namespace DataCollectorService.Worker
{
    public class WorkerService : BackgroundService, ISubject
    {
        private readonly IGeneratorService _generator;
        private readonly List<IObserver> _observers = new();
        private readonly ILogger<WorkerService> _logger;
        private readonly DataCollectorDbContext _dbContext;
        //private readonly List<Patient> _patients = new();
        private readonly MetricGenerationConfig _config;
        private readonly List<IMetricProcessor> _metricProcessors = new();



        private readonly Dictionary<Guid, PatientState> _patientStates = new();
        private List<string> _metricNames;



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

            _metricNames = new List<string>
            {
                "Pulse",
                "Saturation",
                "Respiration",
                "Weight",
                "BMI",
                "Cholesterol",
                "Hemoglobin",
                "SystolicPressure",
                "DiastolicPressure",
                "Temperature"
            };
            //_patients = InitPatients();
        }

        public List<Patient> InitPatients()
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

        public static int CalculateAge(DateTime birthDate)
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


        public void ParseDataFromDTO(PatientDto patientDto, MetricDto metricDto, Patient patient)
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
                    var dtos = await _dbContext.Patients.AsNoTracking().ToListAsync(ct);
                    var allMetrics = await _dbContext.Metrics.AsNoTracking().ToListAsync(ct);

                    var currentPatientIds = dtos.Select(d => d.PatientId).ToList();
                    var removedIds = _patientStates.Keys.Except(currentPatientIds).ToList();
                    foreach (var id in removedIds)
                    {
                        _patientStates.Remove(id);
                        _logger.LogInformation($"Удалено состояние пациента {id}");
                    }
                    
                    var currentPatients = new List<Patient>();

                    foreach (var dto in dtos)
                    {
                        if (!_patientStates.TryGetValue(dto.PatientId, out var state))
                        {
                            state = new PatientState();
                            var now = DateTime.UtcNow;
                            foreach (var metricName in _metricNames)
                            {
                                // инициализируем текущим временем чтоб генерация началась с правильными интервалами
                                state.MetricLastGenerations[metricName] = now;
                            }
                            _patientStates[dto.PatientId] = state;
                            _logger.LogInformation($"Добавлен новый пациент: {dto.PatientId}");
                        }

                        var patientMetrics = allMetrics
                            .Where(m => m.PatientId == dto.PatientId)
                            .ToList();

                        var weightMetric = patientMetrics.FirstOrDefault(m => m.Type == "Weight");

                        var patient = new Patient
                        {
                            Id = dto.PatientId,
                            Age = CalculateAge(dto.BirthDate),
                            Height = dto.Height,
                            Name = $"{dto.FirstName} {dto.MiddleName} {dto.LastName}".Trim(),
                            Sex = dto.Sex.ToString(),
                            Ward = dto.Ward,
                            BaseWeight = weightMetric?.Value ?? 70.0, 
                            MetricLastGenerations = state.MetricLastGenerations
                        };

                        // инициализируем метрики базовыми значениями из БД
                        InitializePatientMetrics(patient, patientMetrics);

                        currentPatients.Add(patient);
                    }

                    await Notify(currentPatients); // Уведомляем наблюдателей о всех пациентах
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в цикле генерации");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }

        private void InitializePatientMetrics(Patient patient, List<MetricDto> patientMetrics)
        {
            // инициализируем метрики базовыми значениями из бдшки или значениями по умолчанию
            foreach (var metric in patientMetrics)
            {
                switch (metric.Type)
                {
                    case "Pulse":
                        patient.Pulse.Value = metric.Value;
                        patient.Pulse.LastUpdate = metric.Timestamp;
                        break;
                    case "Saturation":
                        patient.Saturation.Value = metric.Value;
                        patient.Saturation.LastUpdate = metric.Timestamp;
                        break;
                    case "Temperature":
                        patient.Temperature.Value = metric.Value;
                        patient.Temperature.LastUpdate = metric.Timestamp;
                        break;
                    case "RespirationRate":
                        patient.RespirationRate.Value = metric.Value;
                        patient.RespirationRate.LastUpdate = metric.Timestamp;
                        break;
                    case "SystolicPressure":
                        patient.SystolicPressure.Value = metric.Value;
                        patient.SystolicPressure.LastUpdate = metric.Timestamp;
                        break;
                    case "DiastolicPressure":
                        patient.DiastolicPressure.Value = metric.Value;
                        patient.DiastolicPressure.LastUpdate = metric.Timestamp;
                        break;
                    case "Hemoglobin":
                        patient.Hemoglobin.Value = metric.Value;
                        patient.Hemoglobin.LastUpdate = metric.Timestamp;
                        break;
                    case "Weight":
                        patient.Weight.Value = metric.Value;
                        patient.Weight.LastUpdate = metric.Timestamp;
                        break;
                    case "BMI":
                        patient.BMI.Value = metric.Value;
                        patient.BMI.LastUpdate = metric.Timestamp;
                        break;
                    case "Cholesterol":
                        patient.Cholesterol.Value = metric.Value;
                        patient.Cholesterol.LastUpdate = metric.Timestamp;
                        break;
                }
            }

            // значения по умолчанию для метрик, которых нет в БД
            if (patient.Pulse.Value == 0) patient.Pulse.Value = 70;
            if (patient.Saturation.Value == 0) patient.Saturation.Value = 98;
            if (patient.Temperature.Value == 0) patient.Temperature.Value = 36.6;
            if (patient.RespirationRate.Value == 0) patient.RespirationRate.Value = 16;
            if (patient.SystolicPressure.Value == 0) patient.SystolicPressure.Value = 120;
            if (patient.DiastolicPressure.Value == 0) patient.DiastolicPressure.Value = 80;
            if (patient.Hemoglobin.Value == 0) patient.Hemoglobin.Value = 140;
            if (patient.Weight.Value == 0) patient.Weight.Value = patient.BaseWeight;
            if (patient.BMI.Value == 0 && patient.Height.HasValue) 
            {
                patient.BMI.Value = patient.Weight.Value / Math.Pow(patient.Height.Value / 100, 2);
            }
            if (patient.Cholesterol.Value == 0) patient.Cholesterol.Value = 5.0;
        }

        public async Task Notify(List<Patient> patients)
        {
            var tasks = new List<Task>();
            
            foreach (var observer in _observers)
            {
                tasks.Add(observer.Update(patients));
                //_logger.LogInformation("ГЕНЕРАЦИЯ ИДЕТ АААААААААААААААААААААА"); АХАХХАХАА - зачет
            }
            await Task.WhenAll(tasks);
        }
    }
}
