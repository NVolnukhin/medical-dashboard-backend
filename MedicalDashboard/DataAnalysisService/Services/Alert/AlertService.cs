using DataAnalysisService.Data;
using Shared;

namespace DataAnalysisService.Services.Alert;

public class AlertService : IAlertService
{
    private readonly DataAnalysisDbContext _context;

    public AlertService(DataAnalysisDbContext context)
    {
        _context = context;
    }

    public async Task CreateAlertAsync(AlertDto alertDto)
    {
        _context.Alerts.Add(alertDto);
        await _context.SaveChangesAsync();
    }
} 