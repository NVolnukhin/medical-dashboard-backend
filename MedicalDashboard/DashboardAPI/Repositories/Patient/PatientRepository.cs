using DashboardAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace DashboardAPI.Repositories.Patient;

public class PatientRepository : IPatientRepository
{
    private readonly DashboardDbContext _context;

    public PatientRepository(DashboardDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Models.Patient>> GetAllAsync(string? name = null, int? ward = null, Guid? doctorId = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Patients.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(p => 
                p.FirstName.Contains(name) || 
                p.LastName.Contains(name) || 
                (p.MiddleName != null && p.MiddleName.Contains(name)));
        }

        if (ward.HasValue)
        {
            query = query.Where(p => p.Ward == ward);
        }

        if (doctorId.HasValue)
        {
            query = query.Where(p => p.DoctorId == doctorId);
        }

        return await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Models.Patient?> GetByIdAsync(Guid id)
    {
        return await _context.Patients.FindAsync(id);
    }

    public async Task<Models.Patient> CreateAsync(Models.Patient patient)
    {
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();
        return patient;
    }

    public async Task<Models.Patient> UpdateAsync(Models.Patient patient)
    {
        _context.Patients.Update(patient);
        await _context.SaveChangesAsync();
        return patient;
    }

    public async Task DeleteAsync(Guid id)
    {
        var patient = await _context.Patients.FindAsync(id);
        if (patient != null)
        {
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalCountAsync(string? name = null, int? ward = null, Guid? doctorId = null)
    {
        var query = _context.Patients.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(p => 
                p.FirstName.Contains(name) || 
                p.LastName.Contains(name) || 
                (p.MiddleName != null && p.MiddleName.Contains(name)));
        }

        if (ward.HasValue)
        {
            query = query.Where(p => p.Ward == ward);
        }

        if (doctorId.HasValue)
        {
            query = query.Where(p => p.DoctorId == doctorId);
        }

        return await query.CountAsync();
    }
} 