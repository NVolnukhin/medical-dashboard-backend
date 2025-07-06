using DashboardAPI.DTOs;
using Shared;

namespace DashboardAPI.Services;

public interface IAlertService
{
    Task<IEnumerable<AlertDto>> GetAllAsync(Guid? patientId = null, bool? isProcessed = null, int page = 1, int pageSize = 20);
    Task<AlertDto?> GetByIdAsync(Guid id);
    Task<AlertDto> AcknowledgeAsync(Guid id, Guid acknowledgedBy);
    Task DeleteAsync(Guid id);
    Task<int> GetTotalCountAsync(Guid? patientId = null, bool? isProcessed = null);
} 