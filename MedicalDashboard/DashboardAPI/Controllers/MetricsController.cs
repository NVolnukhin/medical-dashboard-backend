using DashboardAPI.DTOs;
using DashboardAPI.Services;
using DashboardAPI.Services.Metric;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Extensions.Logging;
using Shared;

namespace DashboardAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly IMetricService _metricService;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(IMetricService metricService, ILogger<MetricsController> logger)
    {
        _metricService = metricService;
        _logger = logger;
    }

    /// <summary>
    /// Получить метрики пациента за период
    /// </summary>
    [HttpGet("{patientId}")]
    public async Task<ActionResult<IEnumerable<MetricDto>>> GetMetrics(
        Guid patientId,
        [FromQuery] DateTime? startPeriod,
        [FromQuery] DateTime? endPeriod,
        [FromQuery] string? type)
    {
        _logger.LogInfo($"HTTP GET /api/metrics/{patientId} - получение метрик. Период: {startPeriod} - {endPeriod}, тип: {type}");
        
        try
        {
            var metrics = await _metricService.GetByPatientIdAsync(patientId, startPeriod, endPeriod, type);
            var count  = metrics.Count();
            
            if (count == 0)
            {
                _logger.LogWarning($"HTTP GET /api/metrics/{patientId} - метрики не найдены");
                return NoContent();
            }
            _logger.LogSuccess($"HTTP GET /api/metrics/{patientId} - успешно возвращено {count} метрик");
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"HTTP GET /api/metrics/{patientId} - ошибка при получении метрик", ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить последние метрики пациента
    /// </summary>
    [HttpGet("latest/{patientId}")]
    public async Task<ActionResult<IEnumerable<MetricDto>>> GetLatestMetrics(Guid patientId)
    {
        try
        {
            var metrics = await _metricService.GetLatestByPatientIdAsync(patientId);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Создать новую метрику
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<MetricDto>> CreateMetric([FromBody] MetricDto createDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var metric = await _metricService.CreateAsync(createDto);
            return CreatedAtAction(nameof(GetMetrics), new { patientId = metric.PatientId }, metric);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
} 