using DashboardAPI.Controllers;
using DashboardAPI.Services.Metric;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Shared;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Controllers;

public class MetricsControllerTests
{
    private readonly Mock<IMetricService> _metricServiceMock = new();
    private readonly Mock<ILogger<MetricsController>> _loggerMock = new();
    private readonly MetricsController _controller;

    public MetricsControllerTests()
    {
        _controller = new MetricsController(_metricServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetMetrics_ReturnsOk_WhenMetricsExist()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metrics = new List<MetricDto>
        {
            new() { PatientId = patientId, Type = "HeartRate", Value = 75.0 },
            new() { PatientId = patientId, Type = "BloodPressure", Value = 120.0 }
        };

        _metricServiceMock.Setup(s => s.GetByPatientIdAsync(patientId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync(metrics);

        // Act
        var result = await _controller.GetMetrics(patientId, null, null, null);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMetrics = Assert.IsType<List<MetricDto>>(okResult.Value);
        Assert.Equal(2, returnedMetrics.Count);
    }

    [Fact]
    public async Task GetMetrics_ReturnsNoContent_WhenNoMetricsExist()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var emptyMetrics = new List<MetricDto>();

        _metricServiceMock.Setup(s => s.GetByPatientIdAsync(patientId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ReturnsAsync(emptyMetrics);

        // Act
        var result = await _controller.GetMetrics(patientId, null, null, null);

        // Assert
        Assert.IsType<NoContentResult>(result.Result);
    }

    [Fact]
    public async Task GetMetrics_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _metricServiceMock.Setup(s => s.GetByPatientIdAsync(patientId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetMetrics(patientId, null, null, null);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetMetrics_WithFilters_ReturnsOk()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var startPeriod = DateTime.Now.AddDays(-7);
        var endPeriod = DateTime.Now;
        var type = "HeartRate";
        var metrics = new List<MetricDto>
        {
            new() { PatientId = patientId, Type = type, Value = 75.0 }
        };

        _metricServiceMock.Setup(s => s.GetByPatientIdAsync(patientId, startPeriod, endPeriod, type))
            .ReturnsAsync(metrics);

        // Act
        var result = await _controller.GetMetrics(patientId, startPeriod, endPeriod, type);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMetrics = Assert.IsType<List<MetricDto>>(okResult.Value);
        Assert.Single(returnedMetrics);
    }

    [Fact]
    public async Task GetLatestMetrics_ReturnsOk_WhenMetricsExist()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metrics = new List<MetricDto>
        {
            new() { PatientId = patientId, Type = "HeartRate", Value = 75.0 },
            new() { PatientId = patientId, Type = "BloodPressure", Value = 120.0 }
        };

        _metricServiceMock.Setup(s => s.GetLatestByPatientIdAsync(patientId))
            .ReturnsAsync(metrics);

        // Act
        var result = await _controller.GetLatestMetrics(patientId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedMetrics = Assert.IsType<List<MetricDto>>(okResult.Value);
        Assert.Equal(2, returnedMetrics.Count);
    }

    [Fact]
    public async Task GetLatestMetrics_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _metricServiceMock.Setup(s => s.GetLatestByPatientIdAsync(patientId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetLatestMetrics(patientId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task CreateMetric_ReturnsCreated_WhenMetricIsValid()
    {
        // Arrange
        var metricId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var createDto = new MetricDto { PatientId = patientId, Type = "HeartRate", Value = 75.0 };
        var createdMetric = new MetricDto { PatientId = patientId, Type = "HeartRate", Value = 75.0 };

        _metricServiceMock.Setup(s => s.CreateAsync(createDto))
            .ReturnsAsync(createdMetric);

        // Act
        var result = await _controller.CreateMetric(createDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedMetric = Assert.IsType<MetricDto>(createdResult.Value);
        Assert.Equal(patientId, returnedMetric.PatientId);
        Assert.Equal(nameof(MetricsController.GetMetrics), createdResult.ActionName);
    }

    [Fact]
    public async Task CreateMetric_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        var createDto = new MetricDto { PatientId = Guid.Empty, Type = "", Value = -1 };
        _controller.ModelState.AddModelError("Type", "Тип метрики обязателен");

        // Act
        var result = await _controller.CreateMetric(createDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateMetric_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var createDto = new MetricDto { PatientId = Guid.NewGuid(), Type = "HeartRate", Value = 75.0 };

        _metricServiceMock.Setup(s => s.CreateAsync(createDto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateMetric(createDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
} 