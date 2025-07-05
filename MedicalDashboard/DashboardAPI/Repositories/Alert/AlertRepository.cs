using DashboardAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace DashboardAPI.Repositories.Alert;

public class AlertRepository : IAlertRepository
{
    private readonly DashboardDbContext _context;

    public AlertRepository(DashboardDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Models.Alert>> GetAllAsync(Guid? patientId = null, bool? isProcessed = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Alerts
            .Include(a => a.Patient)
            .AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(a => a.PatientId == patientId);
        }

        if (isProcessed.HasValue)
        {
            query = query.Where(a => a.IsProcessed == isProcessed.Value);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Models.Alert?> GetByIdAsync(Guid id)
    {
        return await _context.Alerts
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Models.Alert> UpdateAsync(Models.Alert alert)
    {
        _context.Alerts.Update(alert);
        await _context.SaveChangesAsync();
        return alert;
    }

    public async Task DeleteAsync(Guid id)
    {
        var alert = await _context.Alerts.FindAsync(id);
        if (alert != null)
        {
            _context.Alerts.Remove(alert);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalCountAsync(Guid? patientId = null, bool? isProcessed = null)
    {
        var query = _context.Alerts.AsQueryable();

        if (patientId.HasValue)
        {
            query = query.Where(a => a.PatientId == patientId);
        }

        if (isProcessed.HasValue)
        {
            query = query.Where(a => a.IsProcessed == isProcessed.Value);
        }

        return await query.CountAsync();
    }
} 