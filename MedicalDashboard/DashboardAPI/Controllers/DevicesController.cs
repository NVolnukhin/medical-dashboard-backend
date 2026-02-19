using DashboardAPI.DTOs;
using DashboardAPI.Services.Device;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DashboardAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(IDeviceService deviceService, ILogger<DevicesController> logger)
    {
        _deviceService = deviceService;
        _logger = logger;
    }

    /// <summary>
    /// Получить список устройств с фильтрацией
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeviceDto>>> GetDevices(
        [FromQuery] int? ward,
        [FromQuery] bool? inUsing)
    {
        try
        {
            var devices = await _deviceService.GetAllAsync(ward, inUsing);
            return Ok(devices);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить информацию об устройстве по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DeviceDto>> GetDevice(Guid id)
    {
        try
        {
            var device = await _deviceService.GetByIdAsync(id);
            if (device == null)
                return NotFound(new { error = "Устройство не найдено" });
            return Ok(device);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить краткую информацию об устройствах, подключённых к пациенту
    /// </summary>
    [HttpGet("on-patient/{id}")]
    public async Task<ActionResult> GetDevicesOnPatient(Guid id)
    {
        try
        {
            var devices = await _deviceService.GetByPatientIdAsync(id);
            // Собираем уникальный массив метрик
            var uniqueMetrics = devices.SelectMany(d => d.ReadableMetrics).Distinct().ToArray();
            return Ok(new { devices, metrics = uniqueMetrics });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Создать новое устройство
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DeviceDto>> CreateDevice([FromBody] ApiDeviceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var device = await _deviceService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetDevice), new { id = device.Id }, device);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Обновить устройство
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<DeviceDto>> UpdateDevice(Guid id, [FromBody] ApiDeviceDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var device = await _deviceService.UpdateAsync(id, dto);
            return Ok(device);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Удалить устройство
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDevice(Guid id)
    {
        try
        {
            await _deviceService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Привязать устройство к пациенту
    /// </summary>
    [HttpPost("attach")]
    public async Task<ActionResult> AttachDevice([FromBody] AttachDeviceDto dto)
    {
        try
        {
            await _deviceService.AttachToPatientAsync(dto.DeviceId, dto.PatientId);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Отвязать устройство от пациента
    /// </summary>
    [HttpPost("{id}/detach")]
    public async Task<ActionResult> DetachDevice(Guid id)
    {
        try
        {
            await _deviceService.DetachFromPatientAsync(id);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить уникальный массив метрик по всем устройствам, подключённым к пациенту
    /// </summary>
    [HttpGet("counting-metrics/{patientId}")]
    public async Task<ActionResult> GetCountingMetrics(Guid patientId)
    {
        try
        {
            var devices = await _deviceService.GetByPatientIdAsync(patientId);
            var metrics = devices.SelectMany(d => d.ReadableMetrics).Distinct().ToArray();
            return Ok(new { metrics });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
} 