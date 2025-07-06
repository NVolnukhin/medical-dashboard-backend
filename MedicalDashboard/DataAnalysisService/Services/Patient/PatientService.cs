using DataAnalysisService.Data;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace DataAnalysisService.Services.Patient;

public class PatientService : IPatientService
{
    private readonly DataAnalysisDbContext _context;

    public PatientService(DataAnalysisDbContext context)
    {
        _context = context;
    }

    public async Task<PatientDto?> GetPatientByIdAsync(Guid patientId)
    {
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.PatientId == patientId);
    }

    public async Task<string> GetPatientFullNameAsync(Guid patientId)
    {
        var patient = await GetPatientByIdAsync(patientId);
        if (patient == null)
            return string.Empty;

        var fullName = patient.FirstName;
        if (!string.IsNullOrEmpty(patient.MiddleName))
            fullName += $" {patient.MiddleName}";
        fullName += $" {patient.LastName}";

        return fullName;
    }
} 