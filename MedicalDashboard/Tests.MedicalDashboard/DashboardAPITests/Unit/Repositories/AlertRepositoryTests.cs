using DashboardAPI.Models;
using DashboardAPI.Repositories.Alert;
using DashboardAPI.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Moq;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Repositories;

public class AlertRepositoryTests
{
    private readonly DbContextOptions<DashboardDbContext> _options;

    public AlertRepositoryTests()
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
    public async Task GetAllAsync_ShouldReturnAllAlerts_WhenNoFilters()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AlertRepository(context);

        var patient1 = new Patient { PatientId = Guid.NewGuid(), FirstName = "Patient 1" };
        var patient2 = new Patient { PatientId = Guid.NewGuid(), FirstName = "Patient 2" };
        context.Patients.AddRange(patient1, patient2);

        var alerts = new List<Alert>
        {
            new Alert { Id = Guid.NewGuid(), PatientId = patient1.PatientId, AlertType = "High Blood Pressure", Indicator = "Systolic", CreatedAt = DateTime.UtcNow, IsProcessed = false },
            new Alert { Id = Guid.NewGuid(), PatientId = patient2.PatientId, AlertType = "Low Heart Rate", Indicator = "BPM", CreatedAt = DateTime.UtcNow, IsProcessed = true },
            new Alert { Id = Guid.NewGuid(), PatientId = patient1.PatientId, AlertType = "High Temperature", Indicator = "Celsius", CreatedAt = DateTime.UtcNow, IsProcessed = false }
        };
        context.Alerts.AddRange(alerts);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByPatientId_WhenProvided()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AlertRepository(context);

        var patient1 = new Patient { PatientId = Guid.NewGuid(), FirstName = "Patient 1" };
        var patient2 = new Patient { PatientId = Guid.NewGuid(), FirstName = "Patient 2" };
        context.Patients.AddRange(patient1, patient2);

        var alerts = new List<Alert>
        {
            new Alert { Id = Guid.NewGuid(), PatientId = patient1.PatientId, AlertType = "High Blood Pressure", Indicator = "Systolic", CreatedAt = DateTime.UtcNow, IsProcessed = false },
            new Alert { Id = Guid.NewGuid(), PatientId = patient2.PatientId, AlertType = "Low Heart Rate", Indicator = "BPM", CreatedAt = DateTime.UtcNow, IsProcessed = true },
            new Alert { Id = Guid.NewGuid(), PatientId = patient1.PatientId, AlertType = "High Temperature", Indicator = "Celsius", CreatedAt = DateTime.UtcNow, IsProcessed = false }
        };
        context.Alerts.AddRange(alerts);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(patientId: patient1.PatientId);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, alert => Assert.Equal(patient1.PatientId, alert.PatientId));
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByIsProcessed_WhenProvided()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AlertRepository(context);

        var patient = new Patient { PatientId = Guid.NewGuid(), FirstName = "Patient 1" };
        context.Patients.Add(patient);

        var alerts = new List<Alert>
        {
            new Alert { Id = Guid.NewGuid(), PatientId = patient.PatientId, AlertType = "High Blood Pressure", Indicator = "Systolic", CreatedAt = DateTime.UtcNow, IsProcessed = false },
            new Alert { Id = Guid.NewGuid(), PatientId = patient.PatientId, AlertType = "Low Heart Rate", Indicator = "BPM", CreatedAt = DateTime.UtcNow, IsProcessed = true },
            new Alert { Id = Guid.NewGuid(), PatientId = patient.PatientId, AlertType = "High Temperature", Indicator = "Celsius", CreatedAt = DateTime.UtcNow, IsProcessed = false }
        };
        context.Alerts.AddRange(alerts);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(isProcessed: false);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, alert => Assert.False(alert.IsProcessed));
    }

    [Fact]
    public async Task GetAllAsync_ShouldApplyPagination_WhenPageAndPageSizeProvided()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AlertRepository(context);

        var patient = new Patient { PatientId = Guid.NewGuid(), FirstName = "Patient 1" };
        context.Patients.Add(patient);

        var alerts = new List<Alert>();
        for (int i = 0; i < 10; i++)
        {
            alerts.Add(new Alert 
            { 
                Id = Guid.NewGuid(), 
                PatientId = patient.PatientId, 
                AlertType = $"Alert {i}", 
                Indicator = $"Indicator {i}", 
                CreatedAt = DateTime.UtcNow.AddDays(-i), 
                IsProcessed = false 
            });
        }
        context.Alerts.AddRange(alerts);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(page: 2, pageSize: 3);

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnAlert_WhenExists()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AlertRepository(context);

        var patient = new Patient { PatientId = Guid.NewGuid(), FirstName = "Patient 1" };
        context.Patients.Add(patient);

        var alert = new Alert 
        { 
            Id = Guid.NewGuid(), 
            PatientId = patient.PatientId, 
            AlertType = "High Blood Pressure", 
            Indicator = "Systolic", 
            CreatedAt = DateTime.UtcNow, 
            IsProcessed = false 
        };
        context.Alerts.Add(alert);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(alert.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(alert.Id, result.Id);
        Assert.Equal(alert.AlertType, result.AlertType);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenAlertDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AlertRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAlert()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AlertRepository(context);

        var patient = new Patient { PatientId = Guid.NewGuid(), FirstName = "Patient 1" };
        context.Patients.Add(patient);

        var alert = new Alert 
        { 
            Id = Guid.NewGuid(), 
            PatientId = patient.PatientId, 
            AlertType = "High Blood Pressure", 
            Indicator = "Systolic", 
            CreatedAt = DateTime.UtcNow, 
            IsProcessed = false 
        };
        context.Alerts.Add(alert);
        await context.SaveChangesAsync();

        // Act
        alert.IsProcessed = true;
        alert.AcknowledgedAt = DateTime.UtcNow;
        var result = await repository.UpdateAsync(alert);

        // Assert
        Assert.True(result.IsProcessed);
        Assert.NotNull(result.AcknowledgedAt);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteAlert_WhenExists()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AlertRepository(context);

        var patient = new Patient { PatientId = Guid.NewGuid(), FirstName = "Patient 1" };
        context.Patients.Add(patient);

        var alert = new Alert 
        { 
            Id = Guid.NewGuid(), 
            PatientId = patient.PatientId, 
            AlertType = "High Blood Pressure", 
            Indicator = "Systolic", 
            CreatedAt = DateTime.UtcNow, 
            IsProcessed = false 
        };
        context.Alerts.Add(alert);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(alert.Id);

        // Assert
        var deletedAlert = await context.Alerts.FindAsync(alert.Id);
        Assert.Null(deletedAlert);
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrowException_WhenAlertDoesNotExist()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AlertRepository(context);

        // Act & Assert
        await repository.DeleteAsync(Guid.NewGuid());
        // Should not throw exception
    }

    [Fact]
    public async Task GetTotalCountAsync_ShouldReturnCorrectCount_WhenNoFilters()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AlertRepository(context);

        var patient = new Patient { PatientId = Guid.NewGuid(), FirstName = "Patient 1" };
        context.Patients.Add(patient);

        var alerts = new List<Alert>
        {
            new Alert { Id = Guid.NewGuid(), PatientId = patient.PatientId, AlertType = "High Blood Pressure", Indicator = "Systolic", CreatedAt = DateTime.UtcNow, IsProcessed = false },
            new Alert { Id = Guid.NewGuid(), PatientId = patient.PatientId, AlertType = "Low Heart Rate", Indicator = "BPM", CreatedAt = DateTime.UtcNow, IsProcessed = true },
            new Alert { Id = Guid.NewGuid(), PatientId = patient.PatientId, AlertType = "High Temperature", Indicator = "Celsius", CreatedAt = DateTime.UtcNow, IsProcessed = false }
        };
        context.Alerts.AddRange(alerts);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetTotalCountAsync();

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task GetTotalCountAsync_ShouldFilterByPatientId_WhenProvided()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AlertRepository(context);

        var patient1 = new Patient { PatientId = Guid.NewGuid(), FirstName = "Patient 1" };
        var patient2 = new Patient { PatientId = Guid.NewGuid(), FirstName = "Patient 2" };
        context.Patients.AddRange(patient1, patient2);

        var alerts = new List<Alert>
        {
            new Alert { Id = Guid.NewGuid(), PatientId = patient1.PatientId, AlertType = "High Blood Pressure", Indicator = "Systolic", CreatedAt = DateTime.UtcNow, IsProcessed = false },
            new Alert { Id = Guid.NewGuid(), PatientId = patient2.PatientId, AlertType = "Low Heart Rate", Indicator = "BPM", CreatedAt = DateTime.UtcNow, IsProcessed = true },
            new Alert { Id = Guid.NewGuid(), PatientId = patient1.PatientId, AlertType = "High Temperature", Indicator = "Celsius", CreatedAt = DateTime.UtcNow, IsProcessed = false }
        };
        context.Alerts.AddRange(alerts);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetTotalCountAsync(patientId: patient1.PatientId);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetTotalCountAsync_ShouldFilterByIsProcessed_WhenProvided()
    {
        // Arrange
        using var context = CreateContext();
        var repository = new AlertRepository(context);

        var patient = new Patient { PatientId = Guid.NewGuid(), FirstName = "Patient 1" };
        context.Patients.Add(patient);

        var alerts = new List<Alert>
        {
            new Alert { Id = Guid.NewGuid(), PatientId = patient.PatientId, AlertType = "High Blood Pressure", Indicator = "Systolic", CreatedAt = DateTime.UtcNow, IsProcessed = false },
            new Alert { Id = Guid.NewGuid(), PatientId = patient.PatientId, AlertType = "Low Heart Rate", Indicator = "BPM", CreatedAt = DateTime.UtcNow, IsProcessed = true },
            new Alert { Id = Guid.NewGuid(), PatientId = patient.PatientId, AlertType = "High Temperature", Indicator = "Celsius", CreatedAt = DateTime.UtcNow, IsProcessed = false }
        };
        context.Alerts.AddRange(alerts);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetTotalCountAsync(isProcessed: false);

        // Assert
        Assert.Equal(2, result);
    }
} 