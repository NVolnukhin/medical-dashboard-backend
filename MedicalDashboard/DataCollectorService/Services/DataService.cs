using DataCollectorService.DCSAppContext;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Extensions.Logging;

namespace DataCollectorService.Services
{
    public class DataService : BackgroundService, IDataService
    {
        private readonly DataCollectorDbContext _dbContext;
        private readonly ILogger<DataService> _logger;
        private List<PatientDto> _patients = new();
        private List<MetricDto> _metrics = new();
        private readonly object _lock = new object();

        public event Action? DataUpdated;

        public DataService(DataCollectorDbContext dbContext, ILogger<DataService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public List<PatientDto> GetPatients()
        {
            lock (_lock)
            {
                var result = _patients.ToList();
                _logger.LogInformation($"GetPatients: возвращаем {result.Count} пациентов");
                return result;
            }
        }

        public List<MetricDto> GetMetrics()
        {
            lock (_lock)
            {
                var result = _metrics.ToList();
                _logger.LogInformation($"GetMetrics: возвращаем {result.Count} метрик");
                return result;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DataService запущен");

            // и пациенты, и метрики
            await LoadInitialData(stoppingToken);

            // далее только пациенты каждые 5 секунд
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    await LoadPatientsOnly(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogFailure("Ошибка в DataService", ex);
                }
            }
        }

        private async Task LoadInitialData(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Загружаем начальные данные из БД (пациенты и метрики)");
                var startTime = DateTime.UtcNow;

                // подключение к БД
                try
                {
                    await _dbContext.Database.CanConnectAsync(cancellationToken);
                    _logger.LogSuccess("Подключение к БД успешно установлено");
                }
                catch (Exception dbEx)
                {
                    _logger.LogFailure("Не удалось подключиться к БД", dbEx);
                    return;
                }

                // загружаем пациентов
                List<PatientDto> patients;
                try
                {
                    patients = await _dbContext.Patients.AsNoTracking().ToListAsync(cancellationToken);
                    _logger.LogInformation($"Загружено {patients.Count} пациентов из БД");
                    
                    if (patients.Count > 0)
                    {
                        _logger.LogInformation($"Пример пациента: ID={patients[0].PatientId}, Имя={patients[0].FirstName} {patients[0].LastName}");
                    }
                }
                catch (Exception pEx)
                {
                    _logger.LogFailure("Ошибка загрузки пациентов из БД", pEx);
                    return;
                }

                // Загружаем метрики
                List<MetricDto> metrics;
                try
                {
                    metrics = await _dbContext.Metrics.AsNoTracking().ToListAsync(cancellationToken);
                    _logger.LogInformation($"Загружено {metrics.Count} метрик из БД");
                    
                    if (metrics.Count > 0)
                    {
                        _logger.LogInformation($"Пример метрики: PatientID={metrics[0].PatientId}, Type={metrics[0].Type}, Value={metrics[0].Value}");
                    }
                }
                catch (Exception mEx)
                {
                    _logger.LogFailure("Ошибка загрузки метрик из БД", mEx);
                    return;
                }

                lock (_lock)
                {
                    _patients = patients;
                    _metrics = metrics;
                }

                var loadTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogSuccess($"Загружено {patients.Count} пациентов и {metrics.Count} метрик за {loadTime:F0}мс");

                DataUpdated?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogFailure("Ошибка загрузки данных из БД", ex);
            }
        }
        
        private async Task LoadPatientsOnly(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Обновляем только список пациентов из БД");
                var startTime = DateTime.UtcNow;

                // подключение к бд
                try
                {
                    if (!await _dbContext.Database.CanConnectAsync(cancellationToken))
                    {
                        _logger.LogWarning("Не удалось подключиться к БД для обновления пациентов");
                        return;
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogFailure("Ошибка при проверке подключения к БД", dbEx);
                    return;
                }

                // пациенты
                List<PatientDto> patients;
                try
                {
                    patients = await _dbContext.Patients.AsNoTracking().ToListAsync(cancellationToken);
                    
                    bool patientsChanged = false;
                    lock (_lock)
                    {
                        var currentIds = _patients.Select(p => p.PatientId).ToHashSet();
                        var newIds = patients.Select(p => p.PatientId).ToHashSet();
                        
                        if (currentIds.Count != newIds.Count || newIds.Except(currentIds).Any() || currentIds.Except(newIds).Any())
                        {
                            patientsChanged = true;
                            _patients = patients;
                        }
                    }
                    
                    if (patientsChanged)
                    {
                        _logger.LogSuccess($"Список пациентов обновлен: {patients.Count} пациентов");
                        DataUpdated?.Invoke();
                    }
                    else
                    {
                        _logger.LogInformation("Список пациентов не изменился");
                    }
                }
                catch (Exception pEx)
                {
                    _logger.LogFailure("Ошибка загрузки пациентов из БД", pEx);
                    return;
                }

                var loadTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation($"Проверка пациентов выполнена за {loadTime:F0}мс");
            }
            catch (Exception ex)
            {
                _logger.LogFailure("Ошибка обновления пациентов из БД", ex);
            }
        }
    }
}