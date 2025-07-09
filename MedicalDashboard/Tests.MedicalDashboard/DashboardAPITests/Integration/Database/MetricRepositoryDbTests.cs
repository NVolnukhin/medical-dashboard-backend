using DashboardAPI.Data;
using DashboardAPI.Repositories.Metric;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.MedicalDashboard.DashboardAPITests.Integration.Database;

public class MetricRepositoryDbTests : IDisposable
{
    private readonly DbContextOptions<DashboardDbContext> _options;
    private readonly DashboardDbContext _context;
    private readonly MetricRepository _repository;
    private readonly Mock<ILogger<MetricRepository>> _loggerMock;

    public MetricRepositoryDbTests()
    {
        _options = new DbContextOptionsBuilder<DashboardDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DashboardDbContext(_options);
        _loggerMock = new Mock<ILogger<MetricRepository>>();
        _repository = new MetricRepository(_context);

        // Создаем тестовые данные
        SeedTestData();
    }

    private void SeedTestData()
    {
        var patientId1 = Guid.NewGuid();
        var patientId2 = Guid.NewGuid();

        var metrics = new List<DashboardAPI.Models.Metric>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = patientId1,
                Type = "HeartRate",
                Value = 75.0,
                Timestamp = DateTime.Now.AddHours(-2)
            },
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = patientId1,
                Type = "BloodPressure",
                Value = 120.0,
                Timestamp = DateTime.Now.AddHours(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = patientId1,
                Type = "HeartRate",
                Value = 78.0,
                Timestamp = DateTime.Now
            },
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = patientId2,
                Type = "Temperature",
                Value = 36.6,
                Timestamp = DateTime.Now.AddHours(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                PatientId = patientId2,
                Type = "HeartRate",
                Value = 72.0,
                Timestamp = DateTime.Now
            }
        };

        _context.Metrics.AddRange(metrics);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByPatientIdAsync_ReturnsAllMetrics_WhenNoFilters()
    {
        // Arrange
        var patientId = _context.Metrics.First().PatientId;

        // Act
        var result = await _repository.GetByPatientIdAsync(patientId);

        // Assert
        var metrics = result.ToList();
        Assert.Equal(3, metrics.Count);
        Assert.All(metrics, m => Assert.Equal(patientId, m.PatientId));
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithTypeFilter_ReturnsFilteredMetrics()
    {
        // Arrange
        var patientId = _context.Metrics.First().PatientId;

        // Act
        var result = await _repository.GetByPatientIdAsync(patientId, type: "HeartRate");

        // Assert
        var metrics = result.ToList();
        Assert.Equal(2, metrics.Count);
        Assert.All(metrics, m => Assert.Equal("HeartRate", m.Type));
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithDateRange_ReturnsFilteredMetrics()
    {
        // Arrange
        var patientId = _context.Metrics.First().PatientId;
        var startPeriod = DateTime.Now.AddHours(-1.5);
        var endPeriod = DateTime.Now.AddHours(-0.5);

        // Act
        var result = await _repository.GetByPatientIdAsync(patientId, startPeriod, endPeriod);

        // Assert
        var metrics = result.ToList();
        Assert.Single(metrics);
        Assert.Equal("BloodPressure", metrics[0].Type);
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithAllFilters_ReturnsFilteredMetrics()
    {
        // Arrange
        var patientId = _context.Metrics.First().PatientId;
        var startPeriod = DateTime.Now.AddHours(-2.5);
        var endPeriod = DateTime.Now;
        var type = "HeartRate";

        // Act
        var result = await _repository.GetByPatientIdAsync(patientId, startPeriod, endPeriod, type);

        // Assert
        var metrics = result.ToList();
        Assert.Equal(2, metrics.Count);
        Assert.All(metrics, m => 
        {
            Assert.Equal(patientId, m.PatientId);
            Assert.Equal(type, m.Type);
            Assert.True(m.Timestamp >= startPeriod && m.Timestamp <= endPeriod);
        });
    }

    [Fact]
    public async Task GetLatestByPatientIdAsync_ReturnsLatestMetrics_ForEachType()
    {
        // Arrange
        var patientId = _context.Metrics.First().PatientId;

        // Act
        var result = await _repository.GetLatestByPatientIdAsync(patientId);

        // Assert
        var metrics = result.ToList();
        Assert.Equal(2, metrics.Count); // HeartRate и BloodPressure (последние для каждого типа)

        var heartRateMetrics = metrics.Where(m => m.Type == "HeartRate").ToList();
        var bloodPressureMetrics = metrics.Where(m => m.Type == "BloodPressure").ToList();

        Assert.Single(heartRateMetrics);
        Assert.Single(bloodPressureMetrics);

        // Проверяем, что это действительно последние метрики
        var latestHeartRate = _context.Metrics
            .Where(m => m.PatientId == patientId && m.Type == "HeartRate")
            .OrderByDescending(m => m.Timestamp)
            .First();

        var latestBloodPressure = _context.Metrics
            .Where(m => m.PatientId == patientId && m.Type == "BloodPressure")
            .OrderByDescending(m => m.Timestamp)
            .First();

        Assert.Equal(latestHeartRate.Id, heartRateMetrics[0].Id);
        Assert.Equal(latestBloodPressure.Id, bloodPressureMetrics[0].Id);
    }

    [Fact]
    public async Task CreateAsync_CreatesNewMetric_WhenValidData()
    {
        // Arrange
        var newMetric = new DashboardAPI.Models.Metric
        {
            PatientId = Guid.NewGuid(),
            Type = "OxygenLevel",
            Value = 98.5,
            Timestamp = DateTime.Now
        };

        // Act
        var result = await _repository.CreateAsync(newMetric);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(newMetric.PatientId, result.PatientId);
        Assert.Equal(newMetric.Type, result.Type);
        Assert.Equal(newMetric.Value, result.Value);

        // Verify it was saved to database
        var savedMetric = await _context.Metrics.FindAsync(result.Id);
        Assert.NotNull(savedMetric);
        Assert.Equal(newMetric.PatientId, savedMetric.PatientId);
        Assert.Equal(newMetric.Type, savedMetric.Type);
        Assert.Equal(newMetric.Value, savedMetric.Value);
    }

    [Fact]
    public async Task CreateAsync_GeneratesNewId_WhenIdIsEmpty()
    {
        // Arrange
        var newMetric = new DashboardAPI.Models.Metric
        {
            Id = Guid.Empty, // Пустой ID
            PatientId = Guid.NewGuid(),
            Type = "Temperature",
            Value = 37.2,
            Timestamp = DateTime.Now
        };

        // Act
        var result = await _repository.CreateAsync(newMetric);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        // Не сравниваем Id, если он генерируется внутри метода
    }

    [Fact]
    public async Task CreateAsync_PreservesExistingId_WhenIdIsProvided()
    {
        // Arrange
        var providedId = Guid.NewGuid();
        var newMetric = new DashboardAPI.Models.Metric
        {
            Id = providedId,
            PatientId = Guid.NewGuid(),
            Type = "BloodPressure",
            Value = 125.0,
            Timestamp = DateTime.Now
        };

        // Act
        var result = await _repository.CreateAsync(newMetric);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(providedId, result.Id);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ReturnsEmptyList_WhenPatientHasNoMetrics()
    {
        // Arrange
        var nonExistentPatientId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByPatientIdAsync(nonExistentPatientId);

        // Assert
        var metrics = result.ToList();
        Assert.Empty(metrics);
    }

    [Fact]
    public async Task GetLatestByPatientIdAsync_ReturnsEmptyList_WhenPatientHasNoMetrics()
    {
        // Arrange
        var nonExistentPatientId = Guid.NewGuid();

        // Act
        var result = await _repository.GetLatestByPatientIdAsync(nonExistentPatientId);

        // Assert
        var metrics = result.ToList();
        Assert.Empty(metrics);
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithInvalidType_ReturnsEmptyList()
    {
        // Arrange
        var patientId = _context.Metrics.First().PatientId;

        // Act
        var result = await _repository.GetByPatientIdAsync(patientId, type: "NonExistentType");

        // Assert
        var metrics = result.ToList();
        Assert.Empty(metrics);
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithFutureDateRange_ReturnsEmptyList()
    {
        // Arrange
        var patientId = _context.Metrics.First().PatientId;
        var futureStart = DateTime.Now.AddDays(1);
        var futureEnd = DateTime.Now.AddDays(2);

        // Act
        var result = await _repository.GetByPatientIdAsync(patientId, futureStart, futureEnd);

        // Assert
        var metrics = result.ToList();
        Assert.Empty(metrics);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
} 