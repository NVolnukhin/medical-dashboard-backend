using DashboardAPI.Repositories.Metric;
using DashboardAPI.Services.Metric;
using DashboardAPI.Services.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Shared;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Services;

public class MetricServiceTests
{
    private readonly Mock<IMetricRepository> _metricRepositoryMock = new();
    private readonly Mock<ISignalRService> _signalRServiceMock = new();
    private readonly Mock<ILogger<MetricService>> _loggerMock = new();
    private readonly MetricService _service;

    public MetricServiceTests()
    {
        _service = new MetricService(_metricRepositoryMock.Object, _signalRServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ReturnsMetrics_WhenMetricsExist()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metrics = new List<DashboardAPI.Models.Metric>
        {
            new() { Id = Guid.NewGuid(), PatientId = patientId, Type = "HeartRate", Value = 75.0, Timestamp = DateTime.Now },
            new() { Id = Guid.NewGuid(), PatientId = patientId, Type = "BloodPressure", Value = 120.0, Timestamp = DateTime.Now }
        };

        _metricRepositoryMock.Setup(r => r.GetByPatientIdAsync(patientId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync(metrics);

        // Act
        var result = await _service.GetByPatientIdAsync(patientId);

        // Assert
        var metricDtos = result.ToList();
        Assert.Equal(2, metricDtos.Count);
        Assert.Equal(metrics[0].PatientId, metricDtos[0].PatientId);
        Assert.Equal(metrics[0].Type, metricDtos[0].Type);
        Assert.Equal(metrics[0].Value, metricDtos[0].Value);
    }

    [Fact]
    public async Task GetByPatientIdAsync_WithFilters_ReturnsFilteredMetrics()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var startPeriod = DateTime.Now.AddDays(-7);
        var endPeriod = DateTime.Now;
        var type = "HeartRate";
        var metrics = new List<DashboardAPI.Models.Metric>
        {
            new() { Id = Guid.NewGuid(), PatientId = patientId, Type = type, Value = 75.0, Timestamp = DateTime.Now }
        };

        _metricRepositoryMock.Setup(r => r.GetByPatientIdAsync(patientId, startPeriod, endPeriod, type))
            .ReturnsAsync(metrics);

        // Act
        var result = await _service.GetByPatientIdAsync(patientId, startPeriod, endPeriod, type);

        // Assert
        var metricDtos = result.ToList();
        Assert.Single(metricDtos);
        Assert.Equal(type, metricDtos[0].Type);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _metricRepositoryMock.Setup(r => r.GetByPatientIdAsync(patientId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetByPatientIdAsync(patientId));
    }

    [Fact]
    public async Task GetLatestByPatientIdAsync_ReturnsLatestMetrics_WhenMetricsExist()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metrics = new List<DashboardAPI.Models.Metric>
        {
            new() { Id = Guid.NewGuid(), PatientId = patientId, Type = "HeartRate", Value = 75.0, Timestamp = DateTime.Now },
            new() { Id = Guid.NewGuid(), PatientId = patientId, Type = "BloodPressure", Value = 120.0, Timestamp = DateTime.Now }
        };

        _metricRepositoryMock.Setup(r => r.GetLatestByPatientIdAsync(patientId))
            .ReturnsAsync(metrics);

        // Act
        var result = await _service.GetLatestByPatientIdAsync(patientId);

        // Assert
        var metricDtos = result.ToList();
        Assert.Equal(2, metricDtos.Count);
        Assert.Equal(metrics[0].PatientId, metricDtos[0].PatientId);
        Assert.Equal(metrics[0].Type, metricDtos[0].Type);
        Assert.Equal(metrics[0].Value, metricDtos[0].Value);
    }

    [Fact]
    public async Task GetLatestByPatientIdAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _metricRepositoryMock.Setup(r => r.GetLatestByPatientIdAsync(patientId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetLatestByPatientIdAsync(patientId));
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedMetric_WhenValidData()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var createDto = new MetricDto
        {
            PatientId = patientId,
            Type = "HeartRate",
            Value = 75.0,
            Timestamp = DateTime.Now
        };

        var createdMetric = new DashboardAPI.Models.Metric
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Type = createDto.Type,
            Value = createDto.Value,
            Timestamp = createDto.Timestamp
        };

        _metricRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<DashboardAPI.Models.Metric>()))
            .ReturnsAsync(createdMetric);
        _signalRServiceMock.Setup(s => s.SendMetricToPatientAsync(patientId, createDto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        Assert.NotNull(result);
        // Не сравниваем Id, если он генерируется внутри метода
        Assert.Equal(createDto.PatientId, result.PatientId);
        Assert.Equal(createDto.Type, result.Type);
        Assert.Equal(createDto.Value, result.Value);
        Assert.Equal(createDto.Timestamp, result.Timestamp);

        _metricRepositoryMock.Verify(r => r.CreateAsync(It.Is<DashboardAPI.Models.Metric>(m => 
            m.PatientId == createDto.PatientId && 
            m.Type == createDto.Type &&
            m.Value == createDto.Value &&
            m.Timestamp == createDto.Timestamp)), Times.Once);

        _signalRServiceMock.Verify(s => s.SendMetricToPatientAsync(patientId, createDto), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var createDto = new MetricDto { PatientId = Guid.NewGuid(), Type = "HeartRate", Value = 75.0 };

        _metricRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<DashboardAPI.Models.Metric>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.CreateAsync(createDto));
    }

    [Fact]
    public async Task CreateAsync_ThrowsException_WhenSignalRServiceThrows()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var createDto = new MetricDto { PatientId = patientId, Type = "HeartRate", Value = 75.0 };
        var createdMetric = new DashboardAPI.Models.Metric { Id = Guid.NewGuid(), PatientId = patientId, Type = createDto.Type, Value = createDto.Value };

        _metricRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<DashboardAPI.Models.Metric>()))
            .ReturnsAsync(createdMetric);
        _signalRServiceMock.Setup(s => s.SendMetricToPatientAsync(patientId, createDto))
            .ThrowsAsync(new Exception("SignalR error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.CreateAsync(createDto));
    }

    [Fact]
    public async Task ProcessMetricFromKafkaAsync_ProcessesMetricSuccessfully_WhenValidData()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var message = new MetricDto
        {
            PatientId = patientId,
            Type = "HeartRate",
            Value = 75.0,
            Timestamp = DateTime.Now
        };

        var createdMetric = new DashboardAPI.Models.Metric
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Type = message.Type,
            Value = message.Value,
            Timestamp = message.Timestamp
        };

        _metricRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<DashboardAPI.Models.Metric>()))
            .ReturnsAsync(createdMetric);
        _signalRServiceMock.Setup(s => s.SendMetricToPatientAsync(patientId, message))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ProcessMetricFromKafkaAsync(message);

        // Assert
        _metricRepositoryMock.Verify(r => r.CreateAsync(It.Is<DashboardAPI.Models.Metric>(m => 
            m.PatientId == message.PatientId && 
            m.Type == message.Type &&
            m.Value == message.Value &&
            m.Timestamp == message.Timestamp)), Times.Once);

        _signalRServiceMock.Verify(s => s.SendMetricToPatientAsync(patientId, message), Times.Once);
    }

    [Fact]
    public async Task ProcessMetricFromKafkaAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var message = new MetricDto { PatientId = Guid.NewGuid(), Type = "HeartRate", Value = 75.0 };

        var createdMetric = new DashboardAPI.Models.Metric
        {
            Id = Guid.NewGuid(),
            PatientId = message.PatientId,
            Type = message.Type,
            Value = message.Value,
            Timestamp = DateTime.Now
        };

        _metricRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<DashboardAPI.Models.Metric>()))
            .ReturnsAsync(createdMetric);
        _signalRServiceMock.Setup(s => s.SendMetricToPatientAsync(message.PatientId, message))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.ProcessMetricFromKafkaAsync(message));
    }

    [Fact]
    public async Task ProcessMetricFromKafkaAsync_ThrowsException_WhenSignalRServiceThrows()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var message = new MetricDto { PatientId = patientId, Type = "HeartRate", Value = 75.0 };

        var createdMetric = new DashboardAPI.Models.Metric
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Type = message.Type,
            Value = message.Value,
            Timestamp = DateTime.Now
        };

        _metricRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<DashboardAPI.Models.Metric>()))
            .ReturnsAsync(createdMetric);
        _signalRServiceMock.Setup(s => s.SendMetricToPatientAsync(patientId, message))
            .ThrowsAsync(new Exception("SignalR error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.ProcessMetricFromKafkaAsync(message));
    }
} 