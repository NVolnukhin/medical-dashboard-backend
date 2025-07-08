using DashboardAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace DashboardAPI.Repositories.Device;

public class DeviceRepository : IDeviceRepository
{
    private readonly DashboardDbContext _context;

    public DeviceRepository(DashboardDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Models.Device>> GetAllAsync(int? ward = null, bool? inUsing = null)
    {
        var query = _context.Devices.AsQueryable();
        if (ward.HasValue)
            query = query.Where(d => d.Ward == ward.Value);
        if (inUsing.HasValue)
            query = query.Where(d => d.InUsing == inUsing.Value);
        return await query.ToListAsync();
    }

    public async Task<Models.Device?> GetByIdAsync(Guid id)
    {
        return await _context.Devices.FindAsync(id);
    }

    public async Task<Models.Device> CreateAsync(Models.Device device)
    {
        _context.Devices.Add(device);
        await _context.SaveChangesAsync();
        return device;
    }

    public async Task<Models.Device> UpdateAsync(Models.Device device)
    {
        _context.Devices.Update(device);
        await _context.SaveChangesAsync();
        return device;
    }

    public async Task DeleteAsync(Guid id)
    {
        var device = await _context.Devices.FindAsync(id);
        if (device != null)
        {
            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
        }
    }

    public async Task AttachToPatientAsync(Guid deviceId, Guid patientId)
    {
        var device = await _context.Devices.FindAsync(deviceId);
        if (device != null)
        {
            device.BusyBy = patientId;
            device.InUsing = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DetachFromPatientAsync(Guid deviceId)
    {
        var device = await _context.Devices.FindAsync(deviceId);
        if (device != null)
        {
            device.BusyBy = null;
            device.InUsing = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Models.Device>> GetByPatientIdAsync(Guid patientId)
    {
        return await _context.Devices.Where(d => d.BusyBy == patientId).ToListAsync();
    }

    public async Task<IEnumerable<string>> GetCountingMetricsAsync()
    {
        // Только по тем, что привязаны к пациентам
        var metrics = await _context.Devices
            .Where(d => d.BusyBy != null)
            .SelectMany(d => d.ReadableMetrics)
            .Distinct()
            .ToListAsync();
        return metrics;
    }
} 