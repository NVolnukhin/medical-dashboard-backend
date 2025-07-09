using DashboardAPI.Controllers;
using DashboardAPI.DTOs;
using DashboardAPI.Services.Device;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Controllers;

public class DevicesControllerTests
{
    private readonly Mock<IDeviceService> _deviceServiceMock = new();
    private readonly Mock<ILogger<DevicesController>> _loggerMock = new();
    private readonly DevicesController _controller;

    public DevicesControllerTests()
    {
        _controller = new DevicesController(_deviceServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetDevices_ReturnsOk_WhenDevicesExist()
    {
        // Arrange
        var devices = new List<DeviceDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Monitor 1", Ward = 101, InUsing = true },
            new() { Id = Guid.NewGuid(), Name = "Monitor 2", Ward = 102, InUsing = false }
        };

        _deviceServiceMock.Setup(s => s.GetAllAsync(It.IsAny<int?>(), It.IsAny<bool?>()))
            .ReturnsAsync(devices);

        // Act
        var result = await _controller.GetDevices(null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDevices = Assert.IsAssignableFrom<IEnumerable<DeviceDto>>(okResult.Value);
        Assert.Equal(2, returnedDevices.Count());
    }

    [Fact]
    public async Task GetDevices_WithFilters_ReturnsOk()
    {
        // Arrange
        var devices = new List<DeviceDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Monitor 1", Ward = 101, InUsing = true }
        };

        _deviceServiceMock.Setup(s => s.GetAllAsync(101, true))
            .ReturnsAsync(devices);

        // Act
        var result = await _controller.GetDevices(101, true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDevices = Assert.IsAssignableFrom<IEnumerable<DeviceDto>>(okResult.Value);
        Assert.Single(returnedDevices);
    }

    [Fact]
    public async Task GetDevices_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _deviceServiceMock.Setup(s => s.GetAllAsync(It.IsAny<int?>(), It.IsAny<bool?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetDevices(null, null);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetDevice_ReturnsOk_WhenDeviceExists()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var device = new DeviceDto { Id = deviceId, Name = "Monitor 1", Ward = 101, InUsing = true };

        _deviceServiceMock.Setup(s => s.GetByIdAsync(deviceId))
            .ReturnsAsync(device);

        // Act
        var result = await _controller.GetDevice(deviceId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDevice = Assert.IsType<DeviceDto>(okResult.Value);
        Assert.Equal(deviceId, returnedDevice.Id);
    }

    [Fact]
    public async Task GetDevice_ReturnsNotFound_WhenDeviceDoesNotExist()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        _deviceServiceMock.Setup(s => s.GetByIdAsync(deviceId))
            .ReturnsAsync((DeviceDto)null);

        // Act
        var result = await _controller.GetDevice(deviceId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var value = notFoundResult.Value;
        var valueDict = value as IDictionary<string, object>;
        if (valueDict != null)
        {
            Assert.Equal("Устройство не найдено", valueDict["error"]);
        }
        else
        {
            var errorProp = value.GetType().GetProperty("error");
            Assert.NotNull(errorProp);
            Assert.Equal("Устройство не найдено", errorProp.GetValue(value)?.ToString());
        }
    }

    [Fact]
    public async Task GetDevice_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        _deviceServiceMock.Setup(s => s.GetByIdAsync(deviceId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetDevice(deviceId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetDevicesOnPatient_ReturnsOk_WhenDevicesExist()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var devices = new List<DeviceShortDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Monitor 1", ReadableMetrics = new List<string> { "HeartRate", "BloodPressure" } },
            new() { Id = Guid.NewGuid(), Name = "Monitor 2", ReadableMetrics = new List<string> { "Temperature", "HeartRate" } }
        };

        _deviceServiceMock.Setup(s => s.GetByPatientIdAsync(patientId))
            .ReturnsAsync(devices);

        // Act
        var result = await _controller.GetDevicesOnPatient(patientId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var valueType = value.GetType();
        var devicesProp = valueType.GetProperty("devices");
        var metricsProp = valueType.GetProperty("metrics");
        Assert.NotNull(devicesProp);
        Assert.NotNull(metricsProp);
        var devicesList = devicesProp.GetValue(value) as IEnumerable<DeviceShortDto>;
        var metrics = metricsProp.GetValue(value) as IEnumerable<string>;
        Assert.NotNull(devicesList);
        Assert.NotNull(metrics);
    }

    [Fact]
    public async Task GetDevicesOnPatient_ReturnsNotFound_WhenPatientDoesNotExist()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _deviceServiceMock.Setup(s => s.GetByPatientIdAsync(patientId))
            .ThrowsAsync(new KeyNotFoundException("Пациент не найден"));

        // Act
        var result = await _controller.GetDevicesOnPatient(patientId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var value = notFoundResult.Value;
        var valueDict = value as IDictionary<string, object>;
        if (valueDict != null)
        {
            Assert.Equal("Пациент не найден", valueDict["error"]);
        }
        else
        {
            var errorProp = value.GetType().GetProperty("error");
            Assert.NotNull(errorProp);
            Assert.Equal("Пациент не найден", errorProp.GetValue(value)?.ToString());
        }
    }

    [Fact]
    public async Task GetDevicesOnPatient_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _deviceServiceMock.Setup(s => s.GetByPatientIdAsync(patientId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetDevicesOnPatient(patientId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task CreateDevice_ReturnsCreated_WhenDeviceIsValid()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var dto = new ApiDeviceDto { Name = "Monitor 1", Ward = 101 };
        var createdDevice = new DeviceDto { Id = deviceId, Name = "Monitor 1", Ward = 101 };

        _deviceServiceMock.Setup(s => s.CreateAsync(dto))
            .ReturnsAsync(createdDevice);

        // Act
        var result = await _controller.CreateDevice(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedDevice = Assert.IsType<DeviceDto>(createdResult.Value);
        Assert.Equal(deviceId, returnedDevice.Id);
        Assert.Equal(nameof(DevicesController.GetDevice), createdResult.ActionName);
    }

    [Fact]
    public async Task CreateDevice_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        var dto = new ApiDeviceDto { Name = "", Ward = -1 };
        _controller.ModelState.AddModelError("Name", "Название обязательно");

        // Act
        var result = await _controller.CreateDevice(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateDevice_ReturnsBadRequest_WhenServiceThrowsArgumentException()
    {
        // Arrange
        var dto = new ApiDeviceDto { Name = "Monitor 1", Ward = 101 };

        _deviceServiceMock.Setup(s => s.CreateAsync(dto))
            .ThrowsAsync(new ArgumentException("Ошибка создания"));

        // Act
        var result = await _controller.CreateDevice(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value;
        var valueDict = value as IDictionary<string, object>;
        if (valueDict != null)
        {
            Assert.Equal("Ошибка создания", valueDict["error"]);
        }
        else
        {
            var errorProp = value.GetType().GetProperty("error");
            Assert.NotNull(errorProp);
            Assert.Equal("Ошибка создания", errorProp.GetValue(value)?.ToString());
        }
    }

    [Fact]
    public async Task CreateDevice_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var dto = new ApiDeviceDto { Name = "Monitor 1", Ward = 101 };

        _deviceServiceMock.Setup(s => s.CreateAsync(dto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateDevice(dto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task UpdateDevice_ReturnsOk_WhenDeviceExists()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var dto = new ApiDeviceDto { Name = "Monitor Updated", Ward = 102 };
        var updatedDevice = new DeviceDto { Id = deviceId, Name = "Monitor Updated", Ward = 102 };

        _deviceServiceMock.Setup(s => s.UpdateAsync(deviceId, dto))
            .ReturnsAsync(updatedDevice);

        // Act
        var result = await _controller.UpdateDevice(deviceId, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDevice = Assert.IsType<DeviceDto>(okResult.Value);
        Assert.Equal(deviceId, returnedDevice.Id);
    }

    [Fact]
    public async Task UpdateDevice_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var dto = new ApiDeviceDto { Name = "", Ward = -1 };
        _controller.ModelState.AddModelError("Name", "Название обязательно");

        // Act
        var result = await _controller.UpdateDevice(deviceId, dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateDevice_ReturnsNotFound_WhenDeviceDoesNotExist()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var dto = new ApiDeviceDto { Name = "Monitor Updated", Ward = 102 };

        _deviceServiceMock.Setup(s => s.UpdateAsync(deviceId, dto))
            .ThrowsAsync(new KeyNotFoundException("Устройство не найдено"));

        // Act
        var result = await _controller.UpdateDevice(deviceId, dto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var value = notFoundResult.Value;
        var valueDict = value as IDictionary<string, object>;
        if (valueDict != null)
        {
            Assert.Equal("Устройство не найдено", valueDict["error"]);
        }
        else
        {
            var errorProp = value.GetType().GetProperty("error");
            Assert.NotNull(errorProp);
            Assert.Equal("Устройство не найдено", errorProp.GetValue(value)?.ToString());
        }
    }

    [Fact]
    public async Task UpdateDevice_ReturnsBadRequest_WhenServiceThrowsArgumentException()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var dto = new ApiDeviceDto { Name = "Monitor", Ward = 101 };
        _deviceServiceMock.Setup(s => s.UpdateAsync(deviceId, dto)).ThrowsAsync(new ArgumentException("Ошибка обновления"));

        // Act
        var result = await _controller.UpdateDevice(deviceId, dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var value = badRequestResult.Value;
        var valueDict = value as IDictionary<string, object>;
        if (valueDict != null)
        {
            Assert.Equal("Ошибка обновления", valueDict["error"]);
        }
        else
        {
            var errorProp = value.GetType().GetProperty("error");
            Assert.NotNull(errorProp);
            Assert.Equal("Ошибка обновления", errorProp.GetValue(value)?.ToString());
        }
    }

    [Fact]
    public async Task UpdateDevice_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var dto = new ApiDeviceDto { Name = "Monitor Updated", Ward = 102 };

        _deviceServiceMock.Setup(s => s.UpdateAsync(deviceId, dto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdateDevice(deviceId, dto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task DeleteDevice_ReturnsNoContent_WhenDeviceExists()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        _deviceServiceMock.Setup(s => s.DeleteAsync(deviceId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteDevice(deviceId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteDevice_ReturnsNotFound_WhenDeviceDoesNotExist()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        _deviceServiceMock.Setup(s => s.DeleteAsync(deviceId))
            .ThrowsAsync(new KeyNotFoundException("Устройство не найдено"));

        // Act
        var result = await _controller.DeleteDevice(deviceId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var value = notFoundResult.Value;
        var valueDict = value as IDictionary<string, object>;
        if (valueDict != null)
        {
            Assert.Equal("Устройство не найдено", valueDict["error"]);
        }
        else
        {
            var errorProp = value.GetType().GetProperty("error");
            Assert.NotNull(errorProp);
            Assert.Equal("Устройство не найдено", errorProp.GetValue(value)?.ToString());
        }
    }

    [Fact]
    public async Task DeleteDevice_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        _deviceServiceMock.Setup(s => s.DeleteAsync(deviceId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteDevice(deviceId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task AttachDevice_ReturnsOk_WhenAttachmentSuccessful()
    {
        // Arrange
        var attachDto = new AttachDeviceDto { DeviceId = Guid.NewGuid(), PatientId = Guid.NewGuid() };

        _deviceServiceMock.Setup(s => s.AttachToPatientAsync(attachDto.DeviceId, attachDto.PatientId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AttachDevice(attachDto);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task AttachDevice_ReturnsNotFound_WhenDeviceOrPatientNotFound()
    {
        // Arrange
        var attachDto = new AttachDeviceDto { DeviceId = Guid.NewGuid(), PatientId = Guid.NewGuid() };

        _deviceServiceMock.Setup(s => s.AttachToPatientAsync(attachDto.DeviceId, attachDto.PatientId))
            .ThrowsAsync(new KeyNotFoundException("Устройство или пациент не найдены"));

        // Act
        var result = await _controller.AttachDevice(attachDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var value = notFoundResult.Value;
        var valueDict = value as IDictionary<string, object>;
        if (valueDict != null)
        {
            Assert.Equal("Устройство или пациент не найдены", valueDict["error"]);
        }
        else
        {
            var errorProp = value.GetType().GetProperty("error");
            Assert.NotNull(errorProp);
            Assert.Equal("Устройство или пациент не найдены", errorProp.GetValue(value)?.ToString());
        }
    }

    [Fact]
    public async Task AttachDevice_ReturnsBadRequest_WhenServiceThrowsArgumentException()
    {
        // Arrange
        var dto = new AttachDeviceDto { DeviceId = Guid.NewGuid(), PatientId = Guid.NewGuid() };

        _deviceServiceMock.Setup(s => s.AttachToPatientAsync(dto.DeviceId, dto.PatientId))
            .ThrowsAsync(new ArgumentException("Ошибка привязки"));

        // Act
        var result = await _controller.AttachDevice(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var value = badRequestResult.Value;
        var valueDict = value as IDictionary<string, object>;
        if (valueDict != null)
        {
            Assert.Equal("Ошибка привязки", valueDict["error"]);
        }
        else
        {
            var errorProp = value.GetType().GetProperty("error");
            Assert.NotNull(errorProp);
            Assert.Equal("Ошибка привязки", errorProp.GetValue(value)?.ToString());
        }
    }

    [Fact]
    public async Task AttachDevice_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var attachDto = new AttachDeviceDto { DeviceId = Guid.NewGuid(), PatientId = Guid.NewGuid() };

        _deviceServiceMock.Setup(s => s.AttachToPatientAsync(attachDto.DeviceId, attachDto.PatientId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.AttachDevice(attachDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task DetachDevice_ReturnsOk_WhenDetachmentSuccessful()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        _deviceServiceMock.Setup(s => s.DetachFromPatientAsync(deviceId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DetachDevice(deviceId);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DetachDevice_ReturnsNotFound_WhenDeviceNotFound()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        _deviceServiceMock.Setup(s => s.DetachFromPatientAsync(deviceId))
            .ThrowsAsync(new KeyNotFoundException("Устройство не найдено"));

        // Act
        var result = await _controller.DetachDevice(deviceId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var value = notFoundResult.Value;
        var valueDict = value as IDictionary<string, object>;
        if (valueDict != null)
        {
            Assert.Equal("Устройство не найдено", valueDict["error"]);
        }
        else
        {
            var errorProp = value.GetType().GetProperty("error");
            Assert.NotNull(errorProp);
            Assert.Equal("Устройство не найдено", errorProp.GetValue(value)?.ToString());
        }
    }

    [Fact]
    public async Task DetachDevice_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var deviceId = Guid.NewGuid();

        _deviceServiceMock.Setup(s => s.DetachFromPatientAsync(deviceId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DetachDevice(deviceId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
} 