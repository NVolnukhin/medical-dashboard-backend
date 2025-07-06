using Shared;

namespace DataAnalysisService.Services.Alert;

public interface IAlertService
{
    Task CreateAlertAsync(AlertDto alertDto);
} 