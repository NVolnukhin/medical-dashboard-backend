namespace DashboardAPI.Repositories.Alert;

public interface IAlertRepository
{
    Task<IEnumerable<Models.Alert>> GetAllAsync(Guid? patientId = null, bool? isProcessed = null, int page = 1, int pageSize = 20);
    Task<Models.Alert?> GetByIdAsync(Guid id);
    Task<Models.Alert> UpdateAsync(Models.Alert alert);
    Task DeleteAsync(Guid id);
    Task<int> GetTotalCountAsync(Guid? patientId = null, bool? isProcessed = null);
} 