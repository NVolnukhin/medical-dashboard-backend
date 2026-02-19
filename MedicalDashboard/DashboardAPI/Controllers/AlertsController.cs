using DashboardAPI.DTOs;
using DashboardAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Extensions.Logging;

namespace DashboardAPI.Controllers;

[ApiController]
[Route("api/patient-alerts")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(IAlertService alertService, ILogger<AlertsController> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    /// <summary>
    /// Получить список алертов с фильтрацией
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertDto>>> GetAlerts(
        [FromQuery] Guid? patientId,
        [FromQuery] bool? isProcessed,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInfo($"HTTP GET /api/alerts - получение списка алертов. Фильтры: patientId={patientId}, isProcessed={isProcessed}, page={page}, pageSize={pageSize}");
        
        try
        {
            var alerts = await _alertService.GetAllAsync(patientId, isProcessed, page, pageSize);
            var totalCount = await _alertService.GetTotalCountAsync(patientId, isProcessed);
            
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page", page.ToString());
            Response.Headers.Append("X-PageSize", pageSize.ToString());
            
            _logger.LogSuccess($"HTTP GET /api/alerts - успешно возвращено {alerts.Count()} алертов из {totalCount}");
            return Ok(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"HTTP GET /api/alerts - ошибка при получении алертов", ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить алерт по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AlertDto>> GetAlert(Guid id)
    {
        try
        {
            var alert = await _alertService.GetByIdAsync(id);
            if (alert == null)
            {
                return NotFound(new { error = "Алерт не найден" });
            }
            
            return Ok(alert);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Обработать алерт (acknowledge)
    /// </summary>
    [HttpPost("{id}/ack")]
    public async Task<ActionResult<AlertDto>> AcknowledgeAlert(Guid id, [FromBody] AcknowledgeAlertDto ackDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var alert = await _alertService.AcknowledgeAsync(id, ackDto.AcknowledgedBy);
            return Ok(alert);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Удалить алерт
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAlert(Guid id)
    {
        try
        {
            await _alertService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
} 