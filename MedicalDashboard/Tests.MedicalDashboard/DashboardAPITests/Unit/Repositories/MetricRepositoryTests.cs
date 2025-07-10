using DashboardAPI.Data;
using DashboardAPI.Models;
using DashboardAPI.Repositories.Metric;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Repositories;

public class MetricRepositoryTests
{
    private readonly DbContextOptions<DashboardDbContext> _options;

    public MetricRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<DashboardDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithValidPatientId_ShouldReturnMetrics()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new MetricRepository(context);

        var patient = new Patient
        {
            PatientId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Test",
            MiddleName = "Test",
            DoctorId = Guid.NewGuid(),
            BirthDate = DateTime.UtcNow.AddYears(-30),
            Sex = 'M'
        };

        var metrics = new List<Metric>
        {
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = patient.PatientId,
                Type = "HeartRate",
                Timestamp = DateTime.UtcNow.AddHours(-1),
                Value = 75.0
            },
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = patient.PatientId,
                Type = "BloodPressure",
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Value = 120.0
            }
        };

        context.Patients.Add(patient);
        context.Metrics.AddRange(metrics);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByPatientIdAsync(patient.PatientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.All(result, metric => Assert.Equal(patient.PatientId, metric.PatientId));
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithNonExistentPatientId_ShouldReturnEmpty()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new MetricRepository(context);

        // Act
        var result = await repository.GetByPatientIdAsync(Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithDateRange_ShouldReturnFilteredMetrics()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new MetricRepository(context);

        var patient = new Patient
        {
            PatientId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Test",
            MiddleName = "Test",
            DoctorId = Guid.NewGuid(),
            BirthDate = DateTime.UtcNow.AddYears(-30),
            Sex = 'M'
        };

        var startDate = DateTime.UtcNow.AddHours(-3);
        var endDate = DateTime.UtcNow.AddHours(-1);

        var metrics = new List<Metric>
        {
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = patient.PatientId,
                Type = "HeartRate",
                Timestamp = DateTime.UtcNow.AddHours(-2), // Within range
                Value = 75.0
            },
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = patient.PatientId,
                Type = "BloodPressure",
                Timestamp = DateTime.UtcNow.AddHours(-4), // Outside range
                Value = 120.0
            },
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = patient.PatientId,
                Type = "HeartRate",
                Timestamp = DateTime.UtcNow, // Outside range
                Value = 80.0
            }
        };

        context.Patients.Add(patient);
        context.Metrics.AddRange(metrics);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByPatientIdAsync(patient.PatientId, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("HeartRate", result.First().Type);
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithTypeFilter_ShouldReturnFilteredMetrics()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new MetricRepository(context);

        var patient = new Patient
        {
            PatientId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Test",
            MiddleName = "Test",
            DoctorId = Guid.NewGuid(),
            BirthDate = DateTime.UtcNow.AddYears(-30),
            Sex = 'M'
        };

        var metrics = new List<Metric>
        {
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = patient.PatientId,
                Type = "HeartRate",
                Timestamp = DateTime.UtcNow.AddHours(-1),
                Value = 75.0
            },
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = patient.PatientId,
                Type = "BloodPressure",
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Value = 120.0
            }
        };

        context.Patients.Add(patient);
        context.Metrics.AddRange(metrics);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByPatientIdAsync(patient.PatientId, type: "HeartRate");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("HeartRate", result.First().Type);
    }

    [Fact]
    public async Task GetLatestByPatientIdAsync_WithValidPatientId_ShouldReturnLatestMetrics()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new MetricRepository(context);

        var patient = new Patient
        {
            PatientId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Test",
            MiddleName = "Test",
            DoctorId = Guid.NewGuid(),
            BirthDate = DateTime.UtcNow.AddYears(-30),
            Sex = 'M'
        };

        var metrics = new List<Metric>
        {
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = patient.PatientId,
                Type = "HeartRate",
                Timestamp = DateTime.UtcNow.AddHours(-1),
                Value = 75.0
            },
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = patient.PatientId,
                Type = "HeartRate",
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Value = 70.0
            },
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = patient.PatientId,
                Type = "BloodPressure",
                Timestamp = DateTime.UtcNow.AddHours(-1),
                Value = 120.0
            }
        };

        context.Patients.Add(patient);
        context.Metrics.AddRange(metrics);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetLatestByPatientIdAsync(patient.PatientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count()); // One for each type
        Assert.Contains(result, m => m.Type == "HeartRate" && m.Value == 75.0);
        Assert.Contains(result, m => m.Type == "BloodPressure" && m.Value == 120.0);
    }

    [Fact]
    public async Task GetLatestByPatientIdAsync_WithNonExistentPatientId_ShouldReturnEmpty()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new MetricRepository(context);

        // Act
        var result = await repository.GetLatestByPatientIdAsync(Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateAsync_WithNullMetric_ShouldThrowNullReferenceException()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new MetricRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            repository.CreateAsync(null!));
    }

    [Fact]
    public async Task CreateManyAsync_WithValidMetrics_ShouldSaveAllToDatabase()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new MetricRepository(context);

        var patient = new Patient
        {
            PatientId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Test",
            MiddleName = "Test",
            DoctorId = Guid.NewGuid(),
            BirthDate = DateTime.UtcNow.AddYears(-30),
            Sex = 'M'
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var metrics = new List<Metric>
        {
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = patient.PatientId,
                Type = "HeartRate",
                Timestamp = DateTime.UtcNow.AddHours(-1),
                Value = 75.0
            },
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = patient.PatientId,
                Type = "BloodPressure",
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Value = 120.0
            }
        };

        // Act
        var result = await repository.CreateManyAsync(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());

        var savedMetrics = await context.Metrics.Where(m => m.PatientId == patient.PatientId).ToListAsync();
        Assert.Equal(2, savedMetrics.Count);
    }

    [Fact]
    public async Task CreateManyAsync_WithEmptyList_ShouldReturnEmpty()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new MetricRepository(context);

        // Act
        var result = await repository.CreateManyAsync(new List<Metric>());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateManyAsync_WithNullMetrics_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new MetricRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            repository.CreateManyAsync(null!));
    }

    [Fact]
    public async Task CreateManyAsync_WithMixedValidAndInvalidMetrics_ShouldCompleteSuccessfully()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new MetricRepository(context);

        var patient = new Patient
        {
            PatientId = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Test",
            MiddleName = "Test",
            DoctorId = Guid.NewGuid(),
            BirthDate = DateTime.UtcNow.AddYears(-30),
            Sex = 'M'
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        var metrics = new List<Metric>
        {
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = patient.PatientId,
                Type = "HeartRate",
                Timestamp = DateTime.UtcNow.AddHours(-1),
                Value = 75.0
            },
            new Metric
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(), // Non-existent patient
                Type = "BloodPressure",
                Timestamp = DateTime.UtcNow.AddHours(-2),
                Value = 120.0
            }
        };

        // Act
        var result = await repository.CreateManyAsync(metrics);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }
} 