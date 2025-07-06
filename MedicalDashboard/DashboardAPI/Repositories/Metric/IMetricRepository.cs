namespace DashboardAPI.Repositories.Metric;

public interface IMetricRepository
{
    Task<IEnumerable<Models.Metric>> GetByPatientIdAsync(Guid patientId, DateTime? startPeriod = null, DateTime? endPeriod = null, string? type = null);
    Task<IEnumerable<Models.Metric>> GetLatestByPatientIdAsync(Guid patientId);
    Task<Models.Metric> CreateAsync(Models.Metric metric);
    Task<IEnumerable<Models.Metric>> CreateManyAsync(IEnumerable<Models.Metric> metrics);
} 