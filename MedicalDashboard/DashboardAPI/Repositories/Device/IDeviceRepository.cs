namespace DashboardAPI.Repositories.Device;

using DashboardAPI.Models;

public interface IDeviceRepository
{
    Task<IEnumerable<Device>> GetAllAsync(int? ward = null, bool? inUsing = null);
    Task<Device?> GetByIdAsync(Guid id);
    Task<Device> CreateAsync(Device device);
    Task<Device> UpdateAsync(Device device);
    Task DeleteAsync(Guid id);
    Task AttachToPatientAsync(Guid deviceId, Guid patientId);
    Task DetachFromPatientAsync(Guid deviceId);
    Task<IEnumerable<Device>> GetByPatientIdAsync(Guid patientId);
    Task<IEnumerable<string>> GetCountingMetricsAsync();
} 