using DashboardAPI.DTOs;
using Shared;

namespace DashboardAPI.Services.Metric;

public interface IMetricService
{
    Task<IEnumerable<MetricDto>> GetByPatientIdAsync(Guid patientId, DateTime? startPeriod = null, DateTime? endPeriod = null, string? type = null);
    Task<IEnumerable<MetricDto>> GetLatestByPatientIdAsync(Guid patientId);
    Task<MetricDto> CreateAsync(MetricDto createDto);
    Task ProcessMetricFromKafkaAsync(MetricDto message);
} 