namespace DashboardAPI.Repositories.Patient;

public interface IPatientRepository
{
    Task<IEnumerable<Models.Patient>> GetAllAsync(string? name = null, int? ward = null, Guid? doctorId = null, int page = 1, int pageSize = 20);
    Task<Models.Patient?> GetByIdAsync(Guid id);
    Task<Models.Patient> CreateAsync(Models.Patient patient);
    Task<Models.Patient> UpdateAsync(Models.Patient patient);
    Task DeleteAsync(Guid id);
    Task<int> GetTotalCountAsync(string? name = null, int? ward = null, Guid? doctorId = null);
} 