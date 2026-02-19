using DashboardAPI.Models;
using DashboardAPI.Repositories.Alert;
using DashboardAPI.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Services;

public class AlertServiceTests
{
    private readonly Mock<IAlertRepository> _alertRepositoryMock = new();
    private readonly Mock<ILogger<AlertService>> _loggerMock = new();
    private readonly AlertService _service;

    public AlertServiceTests()
    {
        _service = new AlertService(_alertRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAlerts_WhenAlertsExist()
    {
        // Arrange
        var alerts = new List<Alert>
        {
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "HighHeartRate", Indicator = "HeartRate", IsProcessed = false },
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "LowBloodPressure", Indicator = "BloodPressure", IsProcessed = true }
        };

        _alertRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(alerts);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        var alertDtos = result.ToList();
        Assert.Equal(2, alertDtos.Count);
        Assert.Equal(alerts[0].Id, alertDtos[0].Id);
        Assert.Equal(alerts[0].AlertType, alertDtos[0].AlertType);
        Assert.Equal(alerts[0].Indicator, alertDtos[0].Indicator);
    }

    [Fact]
    public async Task GetAllAsync_WithFilters_ReturnsFilteredAlerts()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var alerts = new List<Alert>
        {
            new() { Id = Guid.NewGuid(), PatientId = patientId, AlertType = "HighHeartRate", Indicator = "HeartRate", IsProcessed = false }
        };

        _alertRepositoryMock.Setup(r => r.GetAllAsync(patientId, false, 1, 20))
            .ReturnsAsync(alerts);

        // Act
        var result = await _service.GetAllAsync(patientId, false);

        // Assert
        var alertDtos = result.ToList();
        Assert.Single(alertDtos);
        Assert.Equal(patientId, alertDtos[0].PatientId);
        Assert.False(alertDtos[0].IsProcessed);
    }

    [Fact]
    public async Task GetAllAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        _alertRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<Guid?>(), It.IsAny<bool?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetAllAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAlert_WhenAlertExists()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var alert = new Alert { Id = alertId, PatientId = Guid.NewGuid(), AlertType = "HighHeartRate", Indicator = "HeartRate", IsProcessed = false };

        _alertRepositoryMock.Setup(r => r.GetByIdAsync(alertId))
            .ReturnsAsync(alert);

        // Act
        var result = await _service.GetByIdAsync(alertId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(alertId, result.Id);
        Assert.Equal(alert.AlertType, result.AlertType);
        Assert.Equal(alert.Indicator, result.Indicator);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenAlertDoesNotExist()
    {
        // Arrange
        var alertId = Guid.NewGuid();

        _alertRepositoryMock.Setup(r => r.GetByIdAsync(alertId))
            .ReturnsAsync((Alert)null);

        // Act
        var result = await _service.GetByIdAsync(alertId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var alertId = Guid.NewGuid();

        _alertRepositoryMock.Setup(r => r.GetByIdAsync(alertId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetByIdAsync(alertId));
    }

    [Fact]
    public async Task AcknowledgeAsync_ReturnsAcknowledgedAlert_WhenAlertExists()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var acknowledgedBy = Guid.NewGuid();
        var existingAlert = new Alert
        {
            Id = alertId,
            PatientId = Guid.NewGuid(),
            AlertType = "HighHeartRate",
            Indicator = "HeartRate",
            IsProcessed = false,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var updatedAlert = new Alert
        {
            Id = alertId,
            PatientId = existingAlert.PatientId,
            AlertType = existingAlert.AlertType,
            Indicator = existingAlert.Indicator,
            IsProcessed = true,
            AcknowledgedAt = DateTime.UtcNow,
            AcknowledgedBy = acknowledgedBy,
            CreatedAt = existingAlert.CreatedAt
        };

        _alertRepositoryMock.Setup(r => r.GetByIdAsync(alertId))
            .ReturnsAsync(existingAlert);
        _alertRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Alert>()))
            .ReturnsAsync(updatedAlert);

        // Act
        var result = await _service.AcknowledgeAsync(alertId, acknowledgedBy);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(alertId, result.Id);
        Assert.True(result.IsProcessed);
        Assert.Equal(acknowledgedBy, result.AcknowledgedBy);
        Assert.NotNull(result.AcknowledgedAt);

        _alertRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Alert>(a => 
            a.Id == alertId && 
            a.IsProcessed == true &&
            a.AcknowledgedBy == acknowledgedBy &&
            a.AcknowledgedAt.HasValue)), Times.Once);
    }

    [Fact]
    public async Task AcknowledgeAsync_ThrowsArgumentException_WhenAlertDoesNotExist()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var acknowledgedBy = Guid.NewGuid();

        _alertRepositoryMock.Setup(r => r.GetByIdAsync(alertId))
            .ReturnsAsync((Alert)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.AcknowledgeAsync(alertId, acknowledgedBy));
        Assert.Equal($"Алерт с ID {alertId} не найден", exception.Message);
    }

    [Fact]
    public async Task AcknowledgeAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var acknowledgedBy = Guid.NewGuid();
        var existingAlert = new Alert { Id = alertId, PatientId = Guid.NewGuid(), AlertType = "HighHeartRate", IsProcessed = false };

        _alertRepositoryMock.Setup(r => r.GetByIdAsync(alertId))
            .ReturnsAsync(existingAlert);
        _alertRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Alert>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.AcknowledgeAsync(alertId, acknowledgedBy));
    }

    [Fact]
    public async Task DeleteAsync_CompletesSuccessfully_WhenAlertExists()
    {
        // Arrange
        var alertId = Guid.NewGuid();

        _alertRepositoryMock.Setup(r => r.DeleteAsync(alertId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(alertId);

        // Assert
        _alertRepositoryMock.Verify(r => r.DeleteAsync(alertId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var alertId = Guid.NewGuid();

        _alertRepositoryMock.Setup(r => r.DeleteAsync(alertId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.DeleteAsync(alertId));
    }

    [Fact]
    public async Task GetTotalCountAsync_ReturnsCount_WhenValid()
    {
        // Arrange
        var expectedCount = 15;

        _alertRepositoryMock.Setup(r => r.GetTotalCountAsync(It.IsAny<Guid?>(), It.IsAny<bool?>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _service.GetTotalCountAsync();

        // Assert
        Assert.Equal(expectedCount, result);
    }

    [Fact]
    public async Task GetTotalCountAsync_WithFilters_ReturnsFilteredCount()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var expectedCount = 3;

        _alertRepositoryMock.Setup(r => r.GetTotalCountAsync(patientId, false))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _service.GetTotalCountAsync(patientId, false);

        // Assert
        Assert.Equal(expectedCount, result);
    }

    [Fact]
    public async Task GetTotalCountAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        _alertRepositoryMock.Setup(r => r.GetTotalCountAsync(It.IsAny<Guid?>(), It.IsAny<bool?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetTotalCountAsync());
    }
} 