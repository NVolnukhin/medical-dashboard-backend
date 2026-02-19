using DashboardAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace DashboardAPI.Repositories.Metric;

public class MetricRepository : IMetricRepository
{
    private readonly DashboardDbContext _context;

    public MetricRepository(DashboardDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Models.Metric>> GetByPatientIdAsync(Guid patientId, DateTime? startPeriod = null, DateTime? endPeriod = null, string? type = null)
    {
        var query = _context.Metrics.Where(m => m.PatientId == patientId);

        if (startPeriod.HasValue)
        {
            query = query.Where(m => m.Timestamp >= startPeriod.Value);
        }

        if (endPeriod.HasValue)
        {
            query = query.Where(m => m.Timestamp <= endPeriod.Value);
        }

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(m => m.Type == type);
        }

        return await query
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    public async Task<IEnumerable<Models.Metric>> GetLatestByPatientIdAsync(Guid patientId)
    {
        // Получаем последние метрики для каждого типа
        var latestMetrics = await _context.Metrics
            .Where(m => m.PatientId == patientId)
            .GroupBy(m => m.Type)
            .Select(g => g.OrderByDescending(m => m.Timestamp).First())
            .ToListAsync();

        return latestMetrics;
    }

    public async Task<Models.Metric> CreateAsync(Models.Metric metric)
    {
        _context.Metrics.Add(metric);
        await _context.SaveChangesAsync();
        return metric;
    }

    public async Task<IEnumerable<Models.Metric>> CreateManyAsync(IEnumerable<Models.Metric> metrics)
    {
        _context.Metrics.AddRange(metrics);
        await _context.SaveChangesAsync();
        return metrics;
    }
} 