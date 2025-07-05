using DashboardAPI.DTOs;
using DashboardAPI.Services;
using DashboardAPI.Services.Patient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared;
using Shared.Extensions.Logging;

namespace DashboardAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(IPatientService patientService, ILogger<PatientsController> logger)
    {
        _patientService = patientService;
        _logger = logger;
    }

    /// <summary>
    /// Получить список пациентов с фильтрацией и пагинацией
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientDto>>> GetPatients(
        [FromQuery] string? name,
        [FromQuery] int? ward,
        [FromQuery] Guid? doctorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        _logger.LogInfo($"HTTP GET /api/patients - получение списка пациентов. Фильтры: name={name}, ward={ward}, doctorId={doctorId}, page={page}, pageSize={pageSize}");
        
        try
        {
            var patients = await _patientService.GetAllAsync(name, ward, doctorId, page, pageSize);
            var totalCount = await _patientService.GetTotalCountAsync(name, ward, doctorId);
            
            Response.Headers.Append("X-Total-Count", totalCount.ToString());
            Response.Headers.Append("X-Page", page.ToString());
            Response.Headers.Append("X-PageSize", pageSize.ToString());
            
            _logger.LogSuccess($"HTTP GET /api/patients - успешно возвращено {patients.Count()} пациентов из {totalCount}");
            return Ok(patients);
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"HTTP GET /api/patients - ошибка при получении пациентов", ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Получить информацию о пациенте по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PatientDto>> GetPatient(Guid id)
    {
        _logger.LogInfo($"HTTP GET /api/patients/{id} - получение пациента по ID");
        
        try
        {
            var patient = await _patientService.GetByIdAsync(id);
            if (patient == null)
            {
                _logger.LogWarning($"HTTP GET /api/patients/{id} - пациент не найден");
                return NotFound(new { error = "Пациент не найден" });
            }
            
            _logger.LogSuccess($"HTTP GET /api/patients/{id} - пациент успешно найден: {patient.FirstName} {patient.LastName}");
            return Ok(patient);
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"HTTP GET /api/patients/{id} - ошибка при получении пациента", ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Создать нового пациента
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PatientDto>> CreatePatient([FromBody] ApiPatientDto apiDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var patient = await _patientService.CreateAsync(apiDto);
            return CreatedAtAction(nameof(GetPatient), new { id = patient.PatientId }, patient);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Обновить данные пациента
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PatientDto>> UpdatePatient(Guid id, [FromBody] ApiPatientDto updateDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var patient = await _patientService.UpdateAsync(id, updateDto);
            return Ok(patient);
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
    /// Удалить пациента
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeletePatient(Guid id)
    {
        try
        {
            await _patientService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
} 