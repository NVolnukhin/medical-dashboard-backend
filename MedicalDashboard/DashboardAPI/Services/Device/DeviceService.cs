using DashboardAPI.DTOs;
using DashboardAPI.Models;
using DashboardAPI.Repositories.Device;
using DashboardAPI.Repositories.Patient;
using Shared;

namespace DashboardAPI.Services.Device;

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IPatientRepository _patientRepository;

    public DeviceService(IDeviceRepository deviceRepository, IPatientRepository patientRepository)
    {
        _deviceRepository = deviceRepository;
        _patientRepository = patientRepository;
    }

    private static readonly HashSet<string> AllowedMetrics = Enum.GetNames(typeof(MetricType)).ToHashSet();

    private void ValidateMetrics(IEnumerable<string> metrics)
    {
        var invalid = metrics.Where(m => !AllowedMetrics.Contains(m)).ToList();
        if (invalid.Any())
            throw new ArgumentException($"Недопустимые метрики: {string.Join(", ", invalid)}");
    }

    public async Task<IEnumerable<DeviceDto>> GetAllAsync(int? ward = null, bool? inUsing = null)
    {
        var devices = await _deviceRepository.GetAllAsync(ward, inUsing);
        return devices.Select(MapToDto);
    }

    public async Task<DeviceDto?> GetByIdAsync(Guid id)
    {
        var device = await _deviceRepository.GetByIdAsync(id);
        return device == null ? null : MapToDto(device);
    }

    public async Task<DeviceDto> CreateAsync(ApiDeviceDto dto)
    {
        ValidateMetrics(dto.ReadableMetrics);
        var device = new Models.Device
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Ward = dto.Ward,
            InUsing = false,
            ReadableMetrics = dto.ReadableMetrics,
            BusyBy = null
        };
        var created = await _deviceRepository.CreateAsync(device);
        return MapToDto(created);
    }

    public async Task<DeviceDto> UpdateAsync(Guid id, ApiDeviceDto dto)
    {
        ValidateMetrics(dto.ReadableMetrics);
        var device = await _deviceRepository.GetByIdAsync(id);
        if (device == null) throw new KeyNotFoundException("Device not found");
        device.Name = dto.Name;
        device.Ward = dto.Ward;
        device.ReadableMetrics = dto.ReadableMetrics;
        var updated = await _deviceRepository.UpdateAsync(device);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(Guid id)
    {
        var device = await _deviceRepository.GetByIdAsync(id);
        if (device == null) throw new KeyNotFoundException("Device not found");
        await _deviceRepository.DeleteAsync(id);
    }

    public async Task AttachToPatientAsync(Guid deviceId, Guid patientId)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId);
        if (device == null) throw new KeyNotFoundException("Device not found");
        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null) throw new KeyNotFoundException("Patient not found");
        if (device.Ward != (patient.Ward ?? 0))
            throw new ArgumentException("Аппарат и пациент должны находиться в одной палате");
        await _deviceRepository.AttachToPatientAsync(deviceId, patientId);
    }

    public async Task DetachFromPatientAsync(Guid deviceId)
    {
        var device = await _deviceRepository.GetByIdAsync(deviceId);
        if (device == null) throw new KeyNotFoundException("Device not found");
        await _deviceRepository.DetachFromPatientAsync(deviceId);
    }

    public async Task<IEnumerable<DeviceShortDto>> GetByPatientIdAsync(Guid patientId)
    {
        var patient = await _patientRepository.GetByIdAsync(patientId);
        if (patient == null) throw new KeyNotFoundException("Patient not found");
        var devices = await _deviceRepository.GetByPatientIdAsync(patientId);
        return devices.Select(d => new DeviceShortDto
        {
            Id = d.Id,
            Name = d.Name,
            ReadableMetrics = d.ReadableMetrics.Distinct().ToList()
        });
    }

    public async Task<IEnumerable<string>> GetCountingMetricsAsync()
    {
        var devices = await _deviceRepository.GetAllAsync(null, true); // только занятые
        var metrics = devices.SelectMany(d => d.ReadableMetrics).Distinct();
        return metrics;
    }

    private static DeviceDto MapToDto(Models.Device d)
    {
        return new DeviceDto
        {
            Id = d.Id,
            Name = d.Name,
            Ward = d.Ward,
            InUsing = d.InUsing,
            ReadableMetrics = d.ReadableMetrics,
            BusyBy = d.BusyBy
        };
    }
} 