using DashboardAPI.Controllers;
using DashboardAPI.DTOs;
using DashboardAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Shared;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Controllers;

public class AlertsControllerTests
{
    private readonly Mock<IAlertService> _alertServiceMock = new();
    private readonly Mock<ILogger<AlertsController>> _loggerMock = new();
    private readonly AlertsController _controller;

    public AlertsControllerTests()
    {
        _controller = new AlertsController(_alertServiceMock.Object, _loggerMock.Object);
    }
    
    [Fact]
    public async Task GetAlerts_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _alertServiceMock.Setup(s => s.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAlerts(null, null);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetAlert_ReturnsOk_WhenAlertExists()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var alert = new AlertDto { Id = alertId, PatientId = Guid.NewGuid(), AlertType = "HighHeartRate", Indicator = "HeartRate", IsProcessed = false };

        _alertServiceMock.Setup(s => s.GetByIdAsync(alertId))
            .ReturnsAsync(alert);

        // Act
        var result = await _controller.GetAlert(alertId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAlert = Assert.IsType<AlertDto>(okResult.Value);
        Assert.Equal(alertId, returnedAlert.Id);
    }

    [Fact]
    public async Task GetAlert_ReturnsNotFound_WhenAlertDoesNotExist()
    {
        // Arrange
        var alertId = Guid.NewGuid();

        _alertServiceMock.Setup(s => s.GetByIdAsync(alertId))
            .ReturnsAsync((AlertDto)null);

        // Act
        var result = await _controller.GetAlert(alertId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var valueDict = notFoundResult.Value as IDictionary<string, object>;
        if (valueDict != null)
        {
            Assert.Equal("Алерт не найден", valueDict["error"]);
        }
        else
        {
            var errorProp = notFoundResult.Value.GetType().GetProperty("error");
            Assert.NotNull(errorProp);
            Assert.Equal("Алерт не найден", errorProp.GetValue(notFoundResult.Value)?.ToString());
        }
    }

    [Fact]
    public async Task GetAlert_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var alertId = Guid.NewGuid();

        _alertServiceMock.Setup(s => s.GetByIdAsync(alertId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetAlert(alertId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task AcknowledgeAlert_ReturnsOk_WhenAlertExists()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var ackDto = new AcknowledgeAlertDto { AcknowledgedBy = new Guid("251b0d1b-eded-4b85-8ab3-867178f371ae") };
        var acknowledgedAlert = new AlertDto { Id = alertId, PatientId = Guid.NewGuid(), AlertType = "HighHeartRate", Indicator = "HeartRate", IsProcessed = true };

        _alertServiceMock.Setup(s => s.AcknowledgeAsync(alertId, ackDto.AcknowledgedBy))
            .ReturnsAsync(acknowledgedAlert);

        // Act
        var result = await _controller.AcknowledgeAlert(alertId, ackDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAlert = Assert.IsType<AlertDto>(okResult.Value);
        Assert.Equal(alertId, returnedAlert.Id);
        Assert.True(returnedAlert.IsProcessed);
    }

    [Fact]
    public async Task AcknowledgeAlert_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var ackDto = new AcknowledgeAlertDto { AcknowledgedBy = Guid.Empty };
        _controller.ModelState.AddModelError("AcknowledgedBy", "Поле обязательно");

        // Act
        var result = await _controller.AcknowledgeAlert(alertId, ackDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AcknowledgeAlert_ReturnsNotFound_WhenAlertDoesNotExist()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var ackDto = new AcknowledgeAlertDto { AcknowledgedBy = new Guid("251b0d1b-eded-4b85-8ab3-867178f371ae") };

        _alertServiceMock.Setup(s => s.AcknowledgeAsync(alertId, ackDto.AcknowledgedBy))
            .ThrowsAsync(new ArgumentException("Алерт не найден"));

        // Act
        var result = await _controller.AcknowledgeAlert(alertId, ackDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var valueDict = notFoundResult.Value as IDictionary<string, object>;
        if (valueDict != null)
        {
            Assert.Equal("Алерт не найден", valueDict["error"]);
        }
        else
        {
            var errorProp = notFoundResult.Value.GetType().GetProperty("error");
            Assert.NotNull(errorProp);
            Assert.Equal("Алерт не найден", errorProp.GetValue(notFoundResult.Value)?.ToString());
        }
    }

    [Fact]
    public async Task AcknowledgeAlert_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var ackDto = new AcknowledgeAlertDto { AcknowledgedBy = new Guid("251b0d1b-eded-4b85-8ab3-867178f371ae") };

        _alertServiceMock.Setup(s => s.AcknowledgeAsync(alertId, ackDto.AcknowledgedBy))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.AcknowledgeAlert(alertId, ackDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task DeleteAlert_ReturnsNoContent_WhenAlertExists()
    {
        // Arrange
        var alertId = Guid.NewGuid();

        _alertServiceMock.Setup(s => s.DeleteAsync(alertId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteAlert(alertId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteAlert_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var alertId = Guid.NewGuid();

        _alertServiceMock.Setup(s => s.DeleteAsync(alertId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeleteAlert(alertId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
} 