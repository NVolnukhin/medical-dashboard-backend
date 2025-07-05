using DashboardAPI.DTOs;
using DashboardAPI.Repositories;
using DashboardAPI.Repositories.Metric;
using DashboardAPI.Services.SignalR;
using Shared;
using Shared.Extensions.Logging;

namespace DashboardAPI.Services.Metric;

public class MetricService : IMetricService
{
    private readonly IMetricRepository _metricRepository;
    private readonly ISignalRService _signalRService;
    private readonly ILogger<MetricService> _logger;

    public MetricService(IMetricRepository metricRepository, ISignalRService signalRService, ILogger<MetricService> logger)
    {
        _metricRepository = metricRepository;
        _signalRService = signalRService;
        _logger = logger;
    }

    public async Task<IEnumerable<MetricDto>> GetByPatientIdAsync(Guid patientId, DateTime? startPeriod = null, DateTime? endPeriod = null, string? type = null)
    {
        _logger.LogInfo($"Получение метрик для пациента {patientId}. Период: {startPeriod} - {endPeriod}, тип: {type}");
        
        try
        {
            var metrics = await _metricRepository.GetByPatientIdAsync(patientId, startPeriod, endPeriod, type);
            _logger.LogSuccess($"Получено {metrics.Count()} метрик для пациента {patientId}");
            return metrics.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при получении метрик для пациента {patientId}", ex);
            throw;
        }
    }

    public async Task<IEnumerable<MetricDto>> GetLatestByPatientIdAsync(Guid patientId)
    {
        _logger.LogInfo($"Получение последних метрик для пациента {patientId}");
        
        try
        {
            var metrics = await _metricRepository.GetLatestByPatientIdAsync(patientId);
            _logger.LogSuccess($"Получено {metrics.Count()} последних метрик для пациента {patientId}");
            return metrics.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при получении последних метрик для пациента {patientId}", ex);
            throw;
        }
    }

    public async Task<MetricDto> CreateAsync(MetricDto createDto)
    {
        _logger.LogInfo($"Создание новой метрики для пациента {createDto.PatientId}, тип: {createDto.Type}, значение: {createDto.Value}");
        
        try
        {
            var metric = new Models.Metric
            {
                Id = Guid.NewGuid(),
                PatientId = createDto.PatientId,
                Type = createDto.Type,
                Timestamp = createDto.Timestamp,
                Value = createDto.Value
            };

            var createdMetric = await _metricRepository.CreateAsync(metric);
            _logger.LogSuccess($"Метрика успешно создана с ID: {createdMetric.Id}");
            

            await _signalRService.SendMetricToPatientAsync(createDto.PatientId, createDto);
            _logger.LogSuccess($"Метрика отправлена через SignalR для пациента {createDto.PatientId}");
            return MapToDto(createdMetric);
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при создании метрики для пациента {createDto.PatientId}", ex);
            throw;
        }
    }

    public async Task ProcessMetricFromKafkaAsync(MetricDto message)
    {
        _logger.LogInfo($"Обработка метрики из Kafka для пациента {message.PatientId}, тип: {message.Type}, значение: {message.Value}");
        
        try
        {
            var metric = new Models.Metric
            {
                Id = Guid.NewGuid(),
                PatientId = message.PatientId,
                Type = message.Type,
                Timestamp = message.Timestamp,
                Value = message.Value
            };

            await _metricRepository.CreateAsync(metric);
            _logger.LogSuccess($"Метрика из Kafka сохранена с ID: {metric.Id}");

            // Отправляем метрику через SignalR
            await _signalRService.SendMetricToPatientAsync(message.PatientId, message);
            _logger.LogSuccess($"Метрика отправлена через SignalR для пациента {message.PatientId}");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при обработке метрики из Kafka для пациента {message.PatientId}", ex);
            throw;
        }
    }

    private static MetricDto MapToDto(Models.Metric metric)
    {
        return new MetricDto
        {
            PatientId = metric.PatientId,
            Type = metric.Type,
            Timestamp = metric.Timestamp,
            Value = metric.Value
        };
    }
} 