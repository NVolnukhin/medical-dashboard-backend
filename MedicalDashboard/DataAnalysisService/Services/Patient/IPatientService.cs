using Shared;

namespace DataAnalysisService.Services.Patient;

public interface IPatientService
{
    Task<PatientDto?> GetPatientByIdAsync(Guid patientId);
    Task<string> GetPatientFullNameAsync(Guid patientId);
} 