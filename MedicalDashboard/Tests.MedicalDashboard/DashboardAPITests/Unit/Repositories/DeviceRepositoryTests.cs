using DashboardAPI.Models;
using DashboardAPI.Repositories.Device;
using DashboardAPI.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Moq;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Repositories;

public class DeviceRepositoryTests
{
    private readonly DbContextOptions<DashboardDbContext> _options;

    public DeviceRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<DashboardDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private DashboardDbContext CreateContext()
    {
        return new DashboardDbContext(_options);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllDevices_WhenNoFilters()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        var devices = new List<Device>
        {
            new Device { Id = Guid.NewGuid(), Name = "Device 1", Ward = 1, InUsing = false, ReadableMetrics = new List<string> { "Temperature", "BloodPressure" } },
            new Device { Id = Guid.NewGuid(), Name = "Device 2", Ward = 2, InUsing = true, ReadableMetrics = new List<string> { "HeartRate" } },
            new Device { Id = Guid.NewGuid(), Name = "Device 3", Ward = 1, InUsing = false, ReadableMetrics = new List<string> { "OxygenLevel" } }
        };
        context.Devices.AddRange(devices);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByWard_WhenProvided()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        var devices = new List<Device>
        {
            new Device { Id = Guid.NewGuid(), Name = "Device 1", Ward = 1, InUsing = false, ReadableMetrics = new List<string> { "Temperature" } },
            new Device { Id = Guid.NewGuid(), Name = "Device 2", Ward = 2, InUsing = true, ReadableMetrics = new List<string> { "HeartRate" } },
            new Device { Id = Guid.NewGuid(), Name = "Device 3", Ward = 1, InUsing = false, ReadableMetrics = new List<string> { "OxygenLevel" } }
        };
        context.Devices.AddRange(devices);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(ward: 1);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, device => Assert.Equal(1, device.Ward));
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByInUsing_WhenProvided()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        var devices = new List<Device>
        {
            new Device { Id = Guid.NewGuid(), Name = "Device 1", Ward = 1, InUsing = false, ReadableMetrics = new List<string> { "Temperature" } },
            new Device { Id = Guid.NewGuid(), Name = "Device 2", Ward = 2, InUsing = true, ReadableMetrics = new List<string> { "HeartRate" } },
            new Device { Id = Guid.NewGuid(), Name = "Device 3", Ward = 1, InUsing = false, ReadableMetrics = new List<string> { "OxygenLevel" } }
        };
        context.Devices.AddRange(devices);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(inUsing: true);

        // Assert
        Assert.Single(result);
        Assert.True(result.First().InUsing);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDevice_WhenExists()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        var device = new Device 
        { 
            Id = Guid.NewGuid(), 
            Name = "Test Device", 
            Ward = 1, 
            InUsing = false, 
            ReadableMetrics = new List<string> { "Temperature", "BloodPressure" } 
        };
        context.Devices.Add(device);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(device.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(device.Id, result.Id);
        Assert.Equal(device.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenDeviceDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateDevice()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        var device = new Device 
        { 
            Name = "New Device", 
            Ward = 1, 
            InUsing = false, 
            ReadableMetrics = new List<string> { "Temperature" } 
        };

        // Act
        var result = await repository.CreateAsync(device);

        // Assert
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(device.Name, result.Name);
        Assert.Equal(device.Ward, result.Ward);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateDevice()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        var device = new Device 
        { 
            Id = Guid.NewGuid(), 
            Name = "Original Name", 
            Ward = 1, 
            InUsing = false, 
            ReadableMetrics = new List<string> { "Temperature" } 
        };
        context.Devices.Add(device);
        await context.SaveChangesAsync();

        // Act
        device.Name = "Updated Name";
        device.InUsing = true;
        var result = await repository.UpdateAsync(device);

        // Assert
        Assert.Equal("Updated Name", result.Name);
        Assert.True(result.InUsing);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteDevice_WhenExists()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        var device = new Device 
        { 
            Id = Guid.NewGuid(), 
            Name = "Device to Delete", 
            Ward = 1, 
            InUsing = false, 
            ReadableMetrics = new List<string> { "Temperature" } 
        };
        context.Devices.Add(device);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(device.Id);

        // Assert
        var deletedDevice = await context.Devices.FindAsync(device.Id);
        Assert.Null(deletedDevice);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrowException_WhenDeviceDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        // Act & Assert
        await repository.DeleteAsync(Guid.NewGuid());
        // Should not throw exception
    }

    [Fact]
    public async Task AttachToPatientAsync_ShouldAttachDeviceToPatient()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        var device = new Device 
        { 
            Id = Guid.NewGuid(), 
            Name = "Device", 
            Ward = 1, 
            InUsing = false, 
            ReadableMetrics = new List<string> { "Temperature" } 
        };
        context.Devices.Add(device);
        await context.SaveChangesAsync();

        var patientId = Guid.NewGuid();

        // Act
        await repository.AttachToPatientAsync(device.Id, patientId);

        // Assert
        var updatedDevice = await context.Devices.FindAsync(device.Id);
        Assert.NotNull(updatedDevice);
        Assert.Equal(patientId, updatedDevice.BusyBy);
        Assert.True(updatedDevice.InUsing);
    }

    [Fact]
    public async Task DetachFromPatientAsync_ShouldDetachDeviceFromPatient()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        var patientId = Guid.NewGuid();
        var device = new Device 
        { 
            Id = Guid.NewGuid(), 
            Name = "Device", 
            Ward = 1, 
            InUsing = true, 
            BusyBy = patientId,
            ReadableMetrics = new List<string> { "Temperature" } 
        };
        context.Devices.Add(device);
        await context.SaveChangesAsync();

        // Act
        await repository.DetachFromPatientAsync(device.Id);

        // Assert
        var updatedDevice = await context.Devices.FindAsync(device.Id);
        Assert.NotNull(updatedDevice);
        Assert.Null(updatedDevice.BusyBy);
        Assert.False(updatedDevice.InUsing);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ShouldReturnDevicesForPatient()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        var patientId = Guid.NewGuid();
        var devices = new List<Device>
        {
            new Device { Id = Guid.NewGuid(), Name = "Device 1", Ward = 1, InUsing = true, BusyBy = patientId, ReadableMetrics = new List<string> { "Temperature" } },
            new Device { Id = Guid.NewGuid(), Name = "Device 2", Ward = 2, InUsing = true, BusyBy = patientId, ReadableMetrics = new List<string> { "HeartRate" } },
            new Device { Id = Guid.NewGuid(), Name = "Device 3", Ward = 1, InUsing = false, BusyBy = null, ReadableMetrics = new List<string> { "OxygenLevel" } }
        };
        context.Devices.AddRange(devices);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByPatientIdAsync(patientId);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, device => Assert.Equal(patientId, device.BusyBy));
    }

    [Fact]
    public async Task GetByPatientIdAsync_ShouldReturnEmptyList_WhenPatientHasNoDevices()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        // Act
        var result = await repository.GetByPatientIdAsync(Guid.NewGuid());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCountingMetricsAsync_ShouldReturnUniqueMetricsFromAttachedDevices()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        var patientId = Guid.NewGuid();
        var devices = new List<Device>
        {
            new Device { Id = Guid.NewGuid(), Name = "Device 1", Ward = 1, InUsing = true, BusyBy = patientId, ReadableMetrics = new List<string> { "Temperature", "BloodPressure" } },
            new Device { Id = Guid.NewGuid(), Name = "Device 2", Ward = 2, InUsing = true, BusyBy = patientId, ReadableMetrics = new List<string> { "HeartRate", "Temperature" } },
            new Device { Id = Guid.NewGuid(), Name = "Device 3", Ward = 1, InUsing = false, BusyBy = null, ReadableMetrics = new List<string> { "OxygenLevel" } }
        };
        context.Devices.AddRange(devices);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetCountingMetricsAsync();

        // Assert
        Assert.Equal(3, result.Count()); // Temperature, BloodPressure, HeartRate (unique)
        Assert.Contains("Temperature", result);
        Assert.Contains("BloodPressure", result);
        Assert.Contains("HeartRate", result);
        Assert.DoesNotContain("OxygenLevel", result); // Not attached to patient
    }

    [Fact]
    public async Task GetCountingMetricsAsync_ShouldReturnEmptyList_WhenNoDevicesAttached()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new DeviceRepository(context);

        var devices = new List<Device>
        {
            new Device { Id = Guid.NewGuid(), Name = "Device 1", Ward = 1, InUsing = false, BusyBy = null, ReadableMetrics = new List<string> { "Temperature" } },
            new Device { Id = Guid.NewGuid(), Name = "Device 2", Ward = 2, InUsing = false, BusyBy = null, ReadableMetrics = new List<string> { "HeartRate" } }
        };
        context.Devices.AddRange(devices);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetCountingMetricsAsync();

        // Assert
        Assert.Empty(result);
    }
} 