using DashboardAPI.DTOs;
using DashboardAPI.Models;
using DashboardAPI.Repositories;
using DashboardAPI.Repositories.Alert;
using Shared;
using Shared.Extensions.Logging;

namespace DashboardAPI.Services;

public class AlertService : IAlertService
{
    private readonly IAlertRepository _alertRepository;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        IAlertRepository alertRepository, 
        ILogger<AlertService> logger)
    {
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<AlertDto>> GetAllAsync(Guid? patientId = null, bool? isProcessed = null, int page = 1, int pageSize = 20)
    {
        _logger.LogInfo($"Получение списка алертов. Фильтры: patientId={patientId}, isProcessed={isProcessed}, page={page}, pageSize={pageSize}");
        
        try
        {
            var alerts = await _alertRepository.GetAllAsync(patientId, isProcessed, page, pageSize);
            _logger.LogSuccess($"Получено {alerts.Count()} алертов");
            return alerts.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при получении списка алертов", ex);
            throw;
        }
    }

    public async Task<AlertDto?> GetByIdAsync(Guid id)
    {
        _logger.LogInfo($"Получение алерта по ID: {id}");
        
        try
        {
            var alert = await _alertRepository.GetByIdAsync(id);
            if (alert != null)
            {
                _logger.LogSuccess($"Алерт с ID {id} найден: {alert.AlertType} - {alert.Indicator}");
                return MapToDto(alert);
            }
            else
            {
                _logger.LogWarning($"Алерт с ID {id} не найден");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при получении алерта с ID {id}", ex);
            throw;
        }
    }

    
    public async Task<AlertDto> AcknowledgeAsync(Guid id, Guid acknowledgedBy)
    {
        _logger.LogInfo($"Подтверждение алерта с ID: {id} пользователем: {acknowledgedBy}");
        
        try
        {
            var alert = await _alertRepository.GetByIdAsync(id);
            if (alert == null)
            {
                _logger.LogWarning($"Попытка подтверждения несуществующего алерта с ID: {id}");
                throw new ArgumentException($"Алерт с ID {id} не найден");
            }

            alert.AcknowledgedAt = DateTime.UtcNow;
            alert.AcknowledgedBy = acknowledgedBy;
            alert.IsProcessed = true;

            var updatedAlert = await _alertRepository.UpdateAsync(alert);
            _logger.LogSuccess($"Алерт с ID {id} успешно подтвержден пользователем {acknowledgedBy}");
            return MapToDto(updatedAlert);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при подтверждении алерта с ID {id}", ex);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogInfo($"Удаление алерта с ID: {id}");
        
        try
        {
            await _alertRepository.DeleteAsync(id);
            _logger.LogSuccess($"Алерт с ID {id} успешно удален");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при удалении алерта с ID {id}", ex);
            throw;
        }
    }

    public async Task<int> GetTotalCountAsync(Guid? patientId = null, bool? isProcessed = null)
    {
        _logger.LogInfo($"Получение общего количества алертов. Фильтры: patientId={patientId}, isProcessed={isProcessed}");
        
        try
        {
            var count = await _alertRepository.GetTotalCountAsync(patientId, isProcessed);
            _logger.LogInfo($"Общее количество алертов: {count}");
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при получении общего количества алертов", ex);
            throw;
        }
    }



    private static AlertDto MapToDto(Alert alert)
    {
        return new AlertDto
        {
            Id = alert.Id,
            PatientId = alert.PatientId,
            AlertType = alert.AlertType,
            Indicator = alert.Indicator,
            CreatedAt = alert.CreatedAt,
            AcknowledgedAt = alert.AcknowledgedAt,
            AcknowledgedBy = alert.AcknowledgedBy,
            IsProcessed = alert.IsProcessed,
            PatientName = alert.Patient != null ? $"{alert.Patient.FirstName} {alert.Patient.LastName}" : "Unknown"
        };
    }
} 