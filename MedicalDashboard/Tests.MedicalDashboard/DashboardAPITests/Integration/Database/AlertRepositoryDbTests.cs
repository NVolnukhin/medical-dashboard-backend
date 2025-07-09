using DashboardAPI.Data;
using DashboardAPI.Models;
using DashboardAPI.Repositories.Alert;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.MedicalDashboard.DashboardAPITests.Integration.Database;

public class AlertRepositoryDbTests : IDisposable
{
    private readonly DbContextOptions<DashboardDbContext> _options;
    private readonly DashboardDbContext _context;
    private readonly AlertRepository _repository;
    private readonly Mock<ILogger<AlertRepository>> _loggerMock;

    public AlertRepositoryDbTests()
    {
        _options = new DbContextOptionsBuilder<DashboardDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DashboardDbContext(_options);
        _loggerMock = new Mock<ILogger<AlertRepository>>();
        _repository = new AlertRepository(_context);

        // Создаем тестовые данные
        SeedTestData();
    }

    private void SeedTestData()
    {
        var patientId1 = Guid.NewGuid();
        var patientId2 = Guid.NewGuid();
        var acknowledgedBy = Guid.NewGuid();

        // Добавляем пациентов с нужными patientId
        var patients = new List<DashboardAPI.Models.Patient>
        {
            new DashboardAPI.Models.Patient
            {
                PatientId = patientId1,
                FirstName = "Иван",
                LastName = "Иванов",
                DoctorId = Guid.NewGuid(),
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Sex = 'M'
            },
            new DashboardAPI.Models.Patient
            {
                PatientId = patientId2,
                FirstName = "Петр",
                LastName = "Петров",
                DoctorId = Guid.NewGuid(),
                BirthDate = DateTime.UtcNow.AddYears(-40),
                Sex = 'M'
            }
        };
        _context.Patients.AddRange(patients);
        _context.SaveChanges();

        var alerts = new List<Alert>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = patientId1,
                AlertType = "HighHeartRate",
                Indicator = "HeartRate",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                IsProcessed = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = patientId1,
                AlertType = "LowBloodPressure",
                Indicator = "BloodPressure",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                IsProcessed = true,
                AcknowledgedAt = DateTime.UtcNow.AddMinutes(-30),
                AcknowledgedBy = acknowledgedBy
            },
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = patientId2,
                AlertType = "HighTemperature",
                Indicator = "Temperature",
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            }
        };

        _context.Alerts.AddRange(alerts);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAlerts_WhenNoFilters()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        var alerts = result.ToList();
        Assert.Equal(3, alerts.Count);
    }

    [Fact]
    public async Task GetAllAsync_WithPatientIdFilter_ReturnsFilteredAlerts()
    {
        // Arrange
        var patientId = _context.Alerts.First().PatientId;

        // Act
        var result = await _repository.GetAllAsync(patientId);

        // Assert
        var alerts = result.ToList();
        Assert.Equal(2, alerts.Count);
        Assert.All(alerts, a => Assert.Equal(patientId, a.PatientId));
    }

    [Fact]
    public async Task GetAllAsync_WithIsProcessedFilter_ReturnsFilteredAlerts()
    {
        // Act
        var result = await _repository.GetAllAsync(isProcessed: false);

        // Assert
        var alerts = result.ToList();
        Assert.Equal(2, alerts.Count);
        Assert.All(alerts, a => Assert.False(a.IsProcessed));
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var result = await _repository.GetAllAsync(page: 1, pageSize: 2);

        // Assert
        var alerts = result.ToList();
        Assert.Equal(2, alerts.Count);
    }

    [Fact]
    public async Task GetAllAsync_WithAllFilters_ReturnsFilteredAlerts()
    {
        // Arrange
        var patientId = _context.Alerts.First().PatientId;

        // Act
        var result = await _repository.GetAllAsync(patientId, false, 1, 10);

        // Assert
        var alerts = result.ToList();
        Assert.Single(alerts);
        Assert.Equal(patientId, alerts[0].PatientId);
        Assert.False(alerts[0].IsProcessed);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAlert_WhenAlertExists()
    {
        // Arrange
        var existingAlert = _context.Alerts.First();

        // Act
        var result = await _repository.GetByIdAsync(existingAlert.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingAlert.Id, result.Id);
        Assert.Equal(existingAlert.AlertType, result.AlertType);
        Assert.Equal(existingAlert.Indicator, result.Indicator);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenAlertDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }



    [Fact]
    public async Task UpdateAsync_UpdatesExistingAlert_WhenValidData()
    {
        // Arrange
        var existingAlert = _context.Alerts.First();
        var originalAlertType = existingAlert.AlertType;
        existingAlert.AlertType = "UpdatedAlertType";
        existingAlert.IsProcessed = true;
        existingAlert.AcknowledgedAt = DateTime.UtcNow;
        existingAlert.AcknowledgedBy = Guid.NewGuid();

        // Act
        var result = await _repository.UpdateAsync(existingAlert);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("UpdatedAlertType", result.AlertType);
        Assert.True(result.IsProcessed);
        Assert.NotNull(result.AcknowledgedAt);
        Assert.NotNull(result.AcknowledgedBy);

        // Verify it was updated in database
        var updatedAlert = await _context.Alerts.FindAsync(existingAlert.Id);
        Assert.NotNull(updatedAlert);
        Assert.Equal("UpdatedAlertType", updatedAlert.AlertType);
        Assert.True(updatedAlert.IsProcessed);
    }

    [Fact]
    public async Task DeleteAsync_DeletesAlert_WhenAlertExists()
    {
        // Arrange
        var existingAlert = _context.Alerts.First();
        var alertId = existingAlert.Id;

        // Act
        await _repository.DeleteAsync(alertId);

        // Assert
        var deletedAlert = await _context.Alerts.FindAsync(alertId);
        Assert.Null(deletedAlert);
    }

    [Fact]
    public async Task GetTotalCountAsync_ReturnsCorrectCount_WhenNoFilters()
    {
        // Act
        var result = await _repository.GetTotalCountAsync();

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task GetTotalCountAsync_WithPatientIdFilter_ReturnsFilteredCount()
    {
        // Arrange
        var patientId = _context.Alerts.First().PatientId;

        // Act
        var result = await _repository.GetTotalCountAsync(patientId);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetTotalCountAsync_WithIsProcessedFilter_ReturnsFilteredCount()
    {
        // Act
        var result = await _repository.GetTotalCountAsync(isProcessed: true);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task GetTotalCountAsync_WithAllFilters_ReturnsFilteredCount()
    {
        // Arrange
        var patientId = _context.Alerts.First().PatientId;

        // Act
        var result = await _repository.GetTotalCountAsync(patientId, false);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoAlertsExist()
    {
        // Arrange
        _context.Alerts.RemoveRange(_context.Alerts);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        var alerts = result.ToList();
        Assert.Empty(alerts);
    }

    [Fact]
    public async Task GetAllAsync_WithNonExistentPatientId_ReturnsEmptyList()
    {
        // Arrange
        var nonExistentPatientId = Guid.NewGuid();

        // Act
        var result = await _repository.GetAllAsync(nonExistentPatientId);

        // Assert
        var alerts = result.ToList();
        Assert.Empty(alerts);
    }



    public void Dispose()
    {
        _context?.Dispose();
    }
} 