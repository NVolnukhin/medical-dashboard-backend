using DataAnalysisService.Config;
using DataAnalysisService.Services.Alert;
using DataAnalysisService.Services.Kafka;
using DataAnalysisService.Services.Patient;
using DataAnalysisService.Services.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared;
using Shared.Extensions.Logging;
using System.Text.Json;
using DataAnalysisService.DTOs;
using DataAnalysisService.Services.Kafka.Producer;
using Shared.MetricLimits;
using MetricLimits = Shared.MetricLimits.MetricLimits;

namespace DataAnalysisService.Services.Analysis;

public class DataAnalysisService : IDataAnalysisService
{
    private readonly IRedisService _redisService;
    private readonly IPatientService _patientService;
    private readonly IKafkaProducerService _kafkaProducerService;
    private readonly IAlertService _alertService;
    private readonly AnalysisSettings _analysisSettings;
    private readonly MetricsConfig _metricsConfig;
    private readonly ILogger<DataAnalysisService> _logger;

    public DataAnalysisService(
        IRedisService redisService,
        IPatientService patientService,
        IKafkaProducerService kafkaProducerService,
        IAlertService alertService,
        IOptions<AnalysisSettings> analysisSettings,
        IOptions<MetricsConfig> metricsConfig,
        ILogger<DataAnalysisService> logger)
    {
        _redisService = redisService;
        _patientService = patientService;
        _kafkaProducerService = kafkaProducerService;
        _alertService = alertService;
        _analysisSettings = analysisSettings.Value;
        _metricsConfig = metricsConfig.Value;
        _logger = logger;
    }

    public async Task AnalyzeMetricAsync(MetricDto metric)
    {
        try
        {
            if (!Enum.TryParse<MetricType>(metric.Type, out var metricType))
            {
                _logger.LogWarning("Неизвестный тип метрики: {MetricType}", metric.Type);
                return;
            }

            var redisKey = $"patient:{metric.PatientId}:{metric.Type}";
            var previousValue = await _redisService.GetAsync<double?>(redisKey);

            // Сохраняем новое значение в Redis
            await _redisService.SetAsync(redisKey, metric.Value, TimeSpan.FromHours(24));
            
            if (!previousValue.HasValue)
            {
                _logger.LogInfo($"Первое значение метрики для пациента {metric.PatientId}, тип {metric.Type}: {metric.Value}");
                return;
            }
            
            var lastAlertKey = $"lastAlert:{metric.PatientId}:{metric.Type}";
            var lastAlertInfo = await _redisService.GetAsync<LastAlertInfo>(lastAlertKey);
            
            if (lastAlertInfo != null)
            {
                _logger.LogInfo($"Последний алерт для пациента {metric.PatientId}, показатель {metric.Type}: {lastAlertInfo.AlertType} в {lastAlertInfo.Timestamp:HH:mm:ss}");
            }

            var alerts = new List<(string AlertType, string Reason)>();
            var hasAlert = false;

            // Получаем лимиты для данного типа метрики
            var limits = GetMetricLimits(metricType);
            if (limits != null)
            {
                if (metric.Value < limits.Min || metric.Value > limits.Max)
                {
                    if (CanSendAlert(lastAlertInfo, "alert"))
                    {
                        alerts.Add(("alert", $"Value {metric.Value} is outside limits [{limits.Min}, {limits.Max}]"));
                        hasAlert = true;
                    }
                }
            }

            // Проверка на 5%
            var changePercent = Math.Abs((metric.Value - previousValue.Value) / previousValue.Value * 100);
            if (changePercent > _analysisSettings.AlertThresholdPercent)
            {
                if (CanSendAlert(lastAlertInfo, "alert"))
                {
                    alerts.Add(("alert", $"Value changed by {changePercent:F1}% (threshold: {_analysisSettings.AlertThresholdPercent}%)"));
                    hasAlert = true;
                }
            }

            // Проверка на 3% 
            if (!hasAlert && limits != null)
            {
                var range = limits.Max - limits.Min;
                var boundaryThreshold = range * _analysisSettings.WarningBoundaryPercent / 100;

                if (metric.Value <= limits.Min + boundaryThreshold || metric.Value >= limits.Max - boundaryThreshold)
                {
                    if (CanSendAlert(lastAlertInfo, "warning"))
                    {
                        alerts.Add(("warning", $"Value {metric.Value} is close to limits [{limits.Min}, {limits.Max}]"));
                    }
                }
            }

            if (!hasAlert && changePercent > _analysisSettings.WarningThresholdPercent && changePercent <= _analysisSettings.AlertThresholdPercent)
            {
                if (CanSendAlert(lastAlertInfo, "warning"))
                {
                    alerts.Add(("warning", $"Value changed by {changePercent:F1}% (threshold: {_analysisSettings.WarningThresholdPercent}%)"));
                }
            }

            foreach (var (alertType, reason) in alerts)
            {
                await SendAlertAsync(metric.PatientId, metric.Type, alertType, reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка анализа метрики для пациента {metric.PatientId}, тип {metric.Type}", ex);
        }
    }

    private MetricLimits? GetMetricLimits(MetricType metricType)
    {
        return metricType switch
        {
            MetricType.Pulse => _metricsConfig.Pulse,
            MetricType.RespirationRate => _metricsConfig.RespirationRate,
            MetricType.Temperature => _metricsConfig.Temperature,
            MetricType.SystolicPressure => _metricsConfig.SystolicPressure,
            MetricType.DiastolicPressure => _metricsConfig.DiastolicPressure,
            MetricType.Saturation => _metricsConfig.Saturation,
            MetricType.Weight => _metricsConfig.Weight,
            MetricType.Hemoglobin => _metricsConfig.Hemoglobin,
            MetricType.Cholesterol => _metricsConfig.Cholesterol,
            _ => null
        };
    }

    private bool CanSendAlert(LastAlertInfo? lastAlertInfo, string alertType)
    {
        if (lastAlertInfo == null)
        {
            _logger.LogInfo($"Можно отправить {alertType}: нет предыдущих алертов");
            return true;
        }

        var now = DateTime.UtcNow;
        var timeSinceLastAlert = now - lastAlertInfo.Timestamp;

        if (lastAlertInfo.AlertType == alertType)
        {
            var timeoutMinutes = alertType == "alert" 
                ? _analysisSettings.AlertTimeoutMinutes 
                : _analysisSettings.WarningTimeoutMinutes;
            
            var canSend = timeSinceLastAlert.TotalMinutes >= timeoutMinutes;
            _logger.LogInfo($"Можно отправить {alertType}: последний был {lastAlertInfo.AlertType}, прошло {timeSinceLastAlert.TotalMinutes:F1} мин, таймаут {timeoutMinutes} мин -> {canSend}");
            return canSend;
        }

        // Если последний был warning, а сейчас alert - можно отправить alert немедленно
        if (lastAlertInfo.AlertType == "warning" && alertType == "alert")
        {
            _logger.LogInfo($"Можно отправить {alertType}: последний был {lastAlertInfo.AlertType} -> разрешено");
            return true;
        }

        // Если последний был alert, а сейчас warning - нельзя отправить warning
        if (lastAlertInfo.AlertType == "alert" && alertType == "warning")
        {
            _logger.LogInfo($"Можно отправить {alertType}: последний был {lastAlertInfo.AlertType} -> запрещено");
            return false;
        }

        _logger.LogInfo($"Можно отправить {alertType}: неизвестная комбинация -> разрешено");
        return true;
    }

    private async Task SendAlertAsync(Guid patientId, string indicator, string alertType, string reason)
    {
        try
        {
            var patientName = await _patientService.GetPatientFullNameAsync(patientId);
            
            var alertMessage = new PatientAlertMessage
            {
                PatientId = patientId.ToString(),
                PatientName = patientName,
                AlertType = alertType,
                Indicator = indicator
            };

            await _kafkaProducerService.ProduceAsync("md-alerts", alertMessage);

            var alertDto = new AlertDto
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                AlertType = alertType,
                Indicator = indicator,
                CreatedAt = DateTime.UtcNow
            };

            await _alertService.CreateAlertAsync(alertDto);

            // Redis
            var lastAlertKey = $"lastAlert:{patientId}:{indicator}";
            var lastAlertInfo = new LastAlertInfo
            {
                AlertType = alertType,
                Timestamp = DateTime.UtcNow
            };
            await _redisService.SetAsync(lastAlertKey, lastAlertInfo, TimeSpan.FromHours(24));

            _logger.LogSuccess($"Алерт отправлен: {alertType} для пациента {patientId}, показатель {indicator}. Причина: {reason}");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка отправки алерта для пациента {patientId}, показатель {indicator}", ex);
        }
    }
} 