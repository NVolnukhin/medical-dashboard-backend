using DashboardAPI.DTOs;
using DashboardAPI.Repositories.Device;
using DashboardAPI.Repositories.Patient;
using DashboardAPI.Services.Device;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Services;

public class DeviceServiceTests
{
    private readonly Mock<IDeviceRepository> _deviceRepositoryMock = new();
    private readonly Mock<IPatientRepository> _patientRepositoryMock = new();
    private readonly DeviceService _service;

    public DeviceServiceTests()
    {
        _service = new DeviceService(_deviceRepositoryMock.Object, _patientRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsDevices_WhenDevicesExist()
    {
        // Arrange
        var devices = new List<DashboardAPI.Models.Device>
        {
            new() { Id = Guid.NewGuid(), Name = "Monitor 1", Ward = 101, InUsing = true },
            new() { Id = Guid.NewGuid(), Name = "Monitor 2", Ward = 102, InUsing = false }
        };

        _deviceRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<int?>(), It.IsAny<bool?>()))
            .ReturnsAsync(devices);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        var deviceDtos = result.ToList();
        Assert.Equal(2, deviceDtos.Count);
        Assert.Equal(devices[0].Id, deviceDtos[0].Id);
        Assert.Equal(devices[0].Name, deviceDtos[0].Name);
        Assert.Equal(devices[0].Ward, deviceDtos[0].Ward);
    }

    [Fact]
    public async Task GetAllAsync_WithFilters_ReturnsFilteredDevices()
    {
        // Arrange
        var devices = new List<DashboardAPI.Models.Device>
        {
            new() { Id = Guid.NewGuid(), Name = "Monitor 1", Ward = 101, InUsing = true }
        };

        _deviceRepositoryMock.Setup(r => r.GetAllAsync(101, true))
            .ReturnsAsync(devices);

        // Act
        var result = await _service.GetAllAsync(101, true);

        // Assert
        var deviceDtos = result.ToList();
        Assert.Single(deviceDtos);
        Assert.Equal(101, deviceDtos[0].Ward);
        Assert.True(deviceDtos[0].InUsing);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDevice_WhenDeviceExists()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var device = new DashboardAPI.Models.Device { Id = deviceId, Name = "Monitor 1", Ward = 101, InUsing = true };

        _deviceRepositoryMock.Setup(r => r.GetByIdAsync(deviceId))
            .ReturnsAsync(device);

        // Act
        var result = await _service.GetByIdAsync(deviceId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(deviceId, result.Id);
        Assert.Equal(device.Name, result.Name);
        Assert.Equal(device.Ward, result.Ward);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenDeviceDoesNotExist()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        _deviceRepositoryMock.Setup(r => r.GetByIdAsync(deviceId))
            .ReturnsAsync((DashboardAPI.Models.Device)null);

        // Act
        var result = await _service.GetByIdAsync(deviceId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedDevice_WhenValidData()
    {
        // Arrange
        var dto = new ApiDeviceDto
        {
            Name = "Monitor 1",
            Ward = 101,
            ReadableMetrics = new List<string> { "Temperature", "Pulse" }
        };

        var createdDevice = new DashboardAPI.Models.Device
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Ward = dto.Ward,
            InUsing = false,
            ReadableMetrics = dto.ReadableMetrics,
            BusyBy = null
        };

        _deviceRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<DashboardAPI.Models.Device>()))
            .ReturnsAsync(createdDevice);

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdDevice.Id, result.Id);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Ward, result.Ward);
        Assert.False(result.InUsing);
        Assert.Equal(dto.ReadableMetrics, result.ReadableMetrics);

        _deviceRepositoryMock.Verify(r => r.CreateAsync(It.Is<DashboardAPI.Models.Device>(d => 
            d.Name == dto.Name && 
            d.Ward == dto.Ward &&
            d.InUsing == false &&
            d.ReadableMetrics.SequenceEqual(dto.ReadableMetrics))), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ThrowsArgumentException_WhenInvalidMetrics()
    {
        // Arrange
        var dto = new ApiDeviceDto
        {
            Name = "Monitor 1",
            Ward = 101,
            ReadableMetrics = new List<string> { "InvalidMetric", "Pulse" }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(dto));
        Assert.Contains("Недопустимые метрики", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedDevice_WhenDeviceExists()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var existingDevice = new DashboardAPI.Models.Device { Id = deviceId, Name = "Monitor 1", Ward = 101, InUsing = true, ReadableMetrics = new List<string> { "Pulse" } };
        var dto = new ApiDeviceDto
        {
            Name = "Monitor Updated",
            Ward = 102,
            ReadableMetrics = new List<string> { "Temperature", "Pulse" }
        };

        var updatedDevice = new DashboardAPI.Models.Device
        {
            Id = deviceId,
            Name = dto.Name,
            Ward = dto.Ward,
            InUsing = true,
            ReadableMetrics = dto.ReadableMetrics,
            BusyBy = Guid.NewGuid()
        };

        _deviceRepositoryMock.Setup(r => r.GetByIdAsync(deviceId))
            .ReturnsAsync(existingDevice);
        _deviceRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<DashboardAPI.Models.Device>()))
            .ReturnsAsync(updatedDevice);

        // Act
        var result = await _service.UpdateAsync(deviceId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(deviceId, result.Id);
        Assert.Equal(dto.Name, result.Name);
        Assert.Equal(dto.Ward, result.Ward);
        Assert.Equal(dto.ReadableMetrics, result.ReadableMetrics);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsKeyNotFoundException_WhenDeviceDoesNotExist()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var dto = new ApiDeviceDto
        {
            Name = "Monitor Updated",
            Ward = 102,
            ReadableMetrics = new List<string> { "Temperature", "Pulse" }
        };

        _deviceRepositoryMock.Setup(r => r.GetByIdAsync(deviceId))
            .ReturnsAsync((DashboardAPI.Models.Device)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateAsync(deviceId, dto));
    }

    [Fact]
    public async Task UpdateAsync_ThrowsArgumentException_WhenInvalidMetrics()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var existingDevice = new DashboardAPI.Models.Device { Id = deviceId, Name = "Monitor 1", Ward = 101, InUsing = true, ReadableMetrics = new List<string> { "Pulse" } };
        var dto = new ApiDeviceDto
        {
            Name = "Monitor Updated",
            Ward = 102,
            ReadableMetrics = new List<string> { "InvalidMetric", "Pulse" }
        };

        _deviceRepositoryMock.Setup(r => r.GetByIdAsync(deviceId))
            .ReturnsAsync(existingDevice);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(deviceId, dto));
        Assert.Contains("Недопустимые метрики", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_CompletesSuccessfully_WhenDeviceExists()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var device = new DashboardAPI.Models.Device { Id = deviceId, Name = "Monitor 1", Ward = 101 };

        _deviceRepositoryMock.Setup(r => r.GetByIdAsync(deviceId))
            .ReturnsAsync(device);
        _deviceRepositoryMock.Setup(r => r.DeleteAsync(deviceId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(deviceId);

        // Assert
        _deviceRepositoryMock.Verify(r => r.DeleteAsync(deviceId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsKeyNotFoundException_WhenDeviceDoesNotExist()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        _deviceRepositoryMock.Setup(r => r.GetByIdAsync(deviceId))
            .ReturnsAsync((DashboardAPI.Models.Device)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteAsync(deviceId));
    }

    [Fact]
    public async Task AttachToPatientAsync_CompletesSuccessfully_WhenValidData()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var device = new DashboardAPI.Models.Device { Id = deviceId, Name = "Monitor 1", Ward = 101 };
        var patient = new DashboardAPI.Models.Patient { PatientId = patientId, FirstName = "Иван", LastName = "Иванов", Ward = 101 };

        _deviceRepositoryMock.Setup(r => r.GetByIdAsync(deviceId))
            .ReturnsAsync(device);
        _patientRepositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync(patient);
        _deviceRepositoryMock.Setup(r => r.AttachToPatientAsync(deviceId, patientId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.AttachToPatientAsync(deviceId, patientId);

        // Assert
        _deviceRepositoryMock.Verify(r => r.AttachToPatientAsync(deviceId, patientId), Times.Once);
    }

    [Fact]
    public async Task AttachToPatientAsync_ThrowsKeyNotFoundException_WhenDeviceNotFound()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        _deviceRepositoryMock.Setup(r => r.GetByIdAsync(deviceId))
            .ReturnsAsync((DashboardAPI.Models.Device)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AttachToPatientAsync(deviceId, patientId));
    }

    [Fact]
    public async Task AttachToPatientAsync_ThrowsKeyNotFoundException_WhenPatientNotFound()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var device = new DashboardAPI.Models.Device { Id = deviceId, Name = "Monitor 1", Ward = 101 };

        _deviceRepositoryMock.Setup(r => r.GetByIdAsync(deviceId))
            .ReturnsAsync(device);
        _patientRepositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync((DashboardAPI.Models.Patient)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AttachToPatientAsync(deviceId, patientId));
    }

    [Fact]
    public async Task AttachToPatientAsync_ThrowsArgumentException_WhenWardsDoNotMatch()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var device = new DashboardAPI.Models.Device { Id = deviceId, Name = "Monitor 1", Ward = 101 };
        var patient = new DashboardAPI.Models.Patient { PatientId = patientId, FirstName = "Иван", LastName = "Иванов", Ward = 102 };

        _deviceRepositoryMock.Setup(r => r.GetByIdAsync(deviceId))
            .ReturnsAsync(device);
        _patientRepositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync(patient);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.AttachToPatientAsync(deviceId, patientId));
        Assert.Contains("Аппарат и пациент должны находиться в одной палате", exception.Message);
    }

    [Fact]
    public async Task DetachFromPatientAsync_CompletesSuccessfully_WhenDeviceExists()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var device = new DashboardAPI.Models.Device { Id = deviceId, Name = "Monitor 1", Ward = 101 };

        _deviceRepositoryMock.Setup(r => r.GetByIdAsync(deviceId))
            .ReturnsAsync(device);
        _deviceRepositoryMock.Setup(r => r.DetachFromPatientAsync(deviceId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DetachFromPatientAsync(deviceId);

        // Assert
        _deviceRepositoryMock.Verify(r => r.DetachFromPatientAsync(deviceId), Times.Once);
    }

    [Fact]
    public async Task DetachFromPatientAsync_ThrowsKeyNotFoundException_WhenDeviceNotFound()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        _deviceRepositoryMock.Setup(r => r.GetByIdAsync(deviceId))
            .ReturnsAsync((DashboardAPI.Models.Device)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DetachFromPatientAsync(deviceId));
    }

    [Fact]
    public async Task GetByPatientIdAsync_ReturnsDevices_WhenPatientExists()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new DashboardAPI.Models.Patient { PatientId = patientId, FirstName = "Иван", LastName = "Иванов", Ward = 101 };
        var devices = new List<DashboardAPI.Models.Device>
        {
            new() { Id = Guid.NewGuid(), Name = "Monitor 1", ReadableMetrics = new List<string>() { "HeartRate", "BloodPressure" } },
            new() { Id = Guid.NewGuid(), Name = "Monitor 2", ReadableMetrics = new List<string>() { "Temperature", "HeartRate" } }
        };

        _patientRepositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync(patient);
        _deviceRepositoryMock.Setup(r => r.GetByPatientIdAsync(patientId))
            .ReturnsAsync(devices);

        // Act
        var result = await _service.GetByPatientIdAsync(patientId);

        // Assert
        var deviceDtos = result.ToList();
        Assert.Equal(2, deviceDtos.Count);
        Assert.Equal(devices[0].Id, deviceDtos[0].Id);
        Assert.Equal(devices[0].Name, deviceDtos[0].Name);
        Assert.Equal(devices[0].ReadableMetrics, deviceDtos[0].ReadableMetrics);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ThrowsKeyNotFoundException_WhenPatientNotFound()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _patientRepositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync((DashboardAPI.Models.Patient)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetByPatientIdAsync(patientId));
    }

    [Fact]
    public async Task GetCountingMetricsAsync_ReturnsUniqueMetrics_FromBusyDevices()
    {
        // Arrange
        var devices = new List<DashboardAPI.Models.Device>
        {
            new() { Id = Guid.NewGuid(), Name = "Monitor 1", Ward = 101, InUsing = true, ReadableMetrics = new List<string> { "Pulse", "Temperature" } },
            new() { Id = Guid.NewGuid(), Name = "Monitor 2", Ward = 102, InUsing = true, ReadableMetrics = new List<string> { "Temperature", "SystolicPressure" } },
            new() { Id = Guid.NewGuid(), Name = "Monitor 3", Ward = 103, InUsing = true, ReadableMetrics = new List<string> { "Pulse" } }
        };

        _deviceRepositoryMock.Setup(r => r.GetAllAsync(null, true))
            .ReturnsAsync(devices);

        // Act
        var result = await _service.GetCountingMetricsAsync();

        // Assert
        var metrics = result.ToList();
        var expected = new[] { "Pulse", "Temperature", "SystolicPressure" };
        Assert.Equal(expected.Length, metrics.Count);
        foreach (var m in expected)
            Assert.Contains(m, metrics);
    }
} 