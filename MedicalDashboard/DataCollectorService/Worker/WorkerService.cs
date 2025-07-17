using DataCollectorService.Models;
using DataCollectorService.Observerer;
using DataCollectorService.Processors;
using DataCollectorService.Services;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Extensions.Logging;

namespace DataCollectorService.Worker
{
    public class WorkerService : BackgroundService, ISubject
    {
        private readonly IGeneratorService _generator;
        private readonly List<IObserver> _observers = new();
        private readonly ILogger<WorkerService> _logger;
        private readonly IDataService _dataService;
        private readonly MetricGenerationConfig _config;
        private readonly List<IMetricProcessor> _metricProcessors = new();

        private readonly Dictionary<Guid, PatientState> _patientStates = new();
        private List<string> _metricNames;

        public WorkerService(
            IGeneratorService generator,
            ILogger<WorkerService> logger,
            IOptions<MetricGenerationConfig> config,
            IServiceProvider serviceProvider,
            IDataService dataService,
            IEnumerable<IObserver> observers)
        {
            _generator = generator;
            _logger = logger;
            _config = config.Value;
            _dataService = dataService;

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
            var dtos = _dataService.GetPatients();

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

                var metricsDto = _dataService.GetMetrics();
                var weightMetric = metricsDto.FirstOrDefault(m => m.Type == "Weight" && m.PatientId == dto.PatientId);
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

            // Подписываемся на обновление данных
            _dataService.DataUpdated += OnDataUpdated;
            
            // Ждем, пока DataService загрузит данные (максимум 30 секунд)
            var waitStart = DateTime.UtcNow;
            while (_dataService.GetPatients().Count == 0)
            {
                _logger.LogInformation("Ожидаем загрузки данных из БД...");
                await Task.Delay(1000, ct);
                
                if ((DateTime.UtcNow - waitStart).TotalSeconds > 30)
                {
                    _logger.LogWarning("время ожидания загрузки данных из БД более 30 сек");
                    break;
                }
            }

            while (!ct.IsCancellationRequested)
            {
                var cycleStart = DateTime.UtcNow;
                try
                {
                    _logger.LogWarning("Попытка сгенерировать значения");
                    
                    var now = DateTime.UtcNow;
                    var dtos = _dataService.GetPatients();
                    var allMetrics = _dataService.GetMetrics();
                    
                    _logger.LogInformation($"Получено {dtos.Count} пациентов и {allMetrics.Count} метрик из DataService");
                    
                    if (dtos.Count == 0)
                    {
                        _logger.LogWarning("Список пациентов пуст. Пропускаем цикл генерации.");
                        continue;
                    }

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
                    
                    var cycleEnd = DateTime.UtcNow;
                    var cycleTime = (cycleEnd - cycleStart).TotalMilliseconds;
                    _logger.LogSuccess($"Цикл генерации занял {cycleTime:F0}мс");
                }
                catch (Exception ex)
                {
                    _logger.LogFailure("Ошибка в цикле генерации", ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }

            _dataService.DataUpdated -= OnDataUpdated;
        }

        private void OnDataUpdated()
        {
            _logger.LogInformation("Получено уведомление об обновлении данных в БД");
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
            _logger.LogInformation($"Уведомляем {_observers.Count} обсерверов о {patients.Count} пациентах");
            
            if (_observers.Count == 0)
            {
                _logger.LogWarning("Нет зарегистрированных обсерверов!");
                return;
            }
            
            // Запускаем все процессоры параллельно
            var tasks = _observers.Select(observer => observer.Update(patients)).ToArray();
            await Task.WhenAll(tasks);
        }
    }
}
