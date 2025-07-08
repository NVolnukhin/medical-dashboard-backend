using DashboardAPI.DTOs;

namespace DashboardAPI.Services.Device;

public interface IDeviceService
{
    Task<IEnumerable<DeviceDto>> GetAllAsync(int? ward = null, bool? inUsing = null);
    Task<DeviceDto?> GetByIdAsync(Guid id);
    Task<DeviceDto> CreateAsync(ApiDeviceDto dto);
    Task<DeviceDto> UpdateAsync(Guid id, ApiDeviceDto dto);
    Task DeleteAsync(Guid id);
    Task AttachToPatientAsync(Guid deviceId, Guid patientId);
    Task DetachFromPatientAsync(Guid deviceId);
    Task<IEnumerable<DeviceShortDto>> GetByPatientIdAsync(Guid patientId);
    Task<IEnumerable<string>> GetCountingMetricsAsync();
} 