using DashboardAPI.DTOs;
using Shared;

namespace DashboardAPI.Services.Patient;

public interface IPatientService
{
    Task<IEnumerable<PatientDto>> GetAllAsync(string? name = null, int? ward = null, Guid? doctorId = null, int page = 1, int pageSize = 20);
    Task<PatientDto?> GetByIdAsync(Guid id);
    Task<PatientDto> CreateAsync(ApiPatientDto apiDto);
    Task<PatientDto> UpdateAsync(Guid id, ApiPatientDto updateDto);
    Task DeleteAsync(Guid id);
    Task<int> GetTotalCountAsync(string? name = null, int? ward = null, Guid? doctorId = null);
} 