using DataAnalysisService.Config;
using DataAnalysisService.Data;
using DataAnalysisService.Services.Alert;
using DataAnalysisService.Services.Kafka.Producer;
using DataAnalysisService.Services.Patient;
using DataAnalysisService.Services.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shared;
using Shared.MetricLimits;
using Xunit;

namespace Tests.MedicalDashboard.DASTests.Integration.Services;

public class DataAnalysisServiceIntegrationTests
{
    private readonly DbContextOptions<DataAnalysisDbContext> _options;
    private readonly DataAnalysisDbContext _context;
    private readonly Mock<IRedisService> _redisServiceMock;
    private readonly Mock<IKafkaProducerService> _kafkaProducerServiceMock;
    private readonly Mock<ILogger<DataAnalysisService.Services.Analysis.DataAnalysisService>> _loggerMock;
    private readonly AnalysisSettings _analysisSettings;
    private readonly MetricsConfig _metricsConfig;
    private readonly PatientService _patientService;
    private readonly AlertService _alertService;
    private readonly DataAnalysisService.Services.Analysis.DataAnalysisService _dataAnalysisService;

    public DataAnalysisServiceIntegrationTests()
    {
        _options = new DbContextOptionsBuilder<DataAnalysisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataAnalysisDbContext(_options);
        _redisServiceMock = new Mock<IRedisService>();
        _kafkaProducerServiceMock = new Mock<IKafkaProducerService>();
        _loggerMock = new Mock<ILogger<DataAnalysisService.Services.Analysis.DataAnalysisService>>();

        _analysisSettings = new AnalysisSettings
        {
            AlertThresholdPercent = 5.0,
            WarningThresholdPercent = 3.0,
            WarningBoundaryPercent = 3.0,
            WarningTimeoutMinutes = 10,
            AlertTimeoutMinutes = 5
        };

        _metricsConfig = new MetricsConfig
        {
            Pulse = new MetricLimits { Min = 60, Max = 100 },
            Temperature = new MetricLimits { Min = 36.0, Max = 37.5 },
            SystolicPressure = new MetricLimits { Min = 90, Max = 140 },
            DiastolicPressure = new MetricLimits { Min = 60, Max = 90 },
            RespirationRate = new MetricLimits { Min = 12, Max = 20 },
            Saturation = new MetricLimits { Min = 95, Max = 100 },
            Weight = new MetricLimits { Min = 40, Max = 150 },
            Hemoglobin = new MetricLimits { Min = 120, Max = 160 },
            Cholesterol = new MetricLimits { Min = 3.0, Max = 5.2 },
            BMI = new MetricLimits { Min = 18.5, Max = 25.0 }
        };

        var analysisSettingsOptions = Options.Create(_analysisSettings);
        var metricsConfigOptions = Options.Create(_metricsConfig);

        _patientService = new PatientService(_context);
        _alertService = new AlertService(_context);

        _dataAnalysisService = new DataAnalysisService.Services.Analysis.DataAnalysisService(
            _redisServiceMock.Object,
            _patientService,
            _kafkaProducerServiceMock.Object,
            _alertService,
            analysisSettingsOptions,
            metricsConfigOptions,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task AnalyzeMetricAsync_WithRealPatientAndAlertServices_ProcessesCorrectly()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new PatientDto
        {
            PatientId = patientId,
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Петрович",
            Ward = 101
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 150 // За пределами лимитов
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<DataAnalysisService.DTOs.LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync((DataAnalysisService.DTOs.LastAlertInfo?)null);
        _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<DataAnalysisService.DTOs.LastAlertInfo>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        _kafkaProducerServiceMock.Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<PatientAlertMessage>()))
            .Returns(Task.CompletedTask);

        // Act
        await _dataAnalysisService.AnalyzeMetricAsync(metric);

        // Assert
        var savedAlerts = await _context.Alerts.Where(a => a.PatientId == patientId).ToListAsync();
        Assert.Single(savedAlerts);
        
        var savedAlert = savedAlerts[0];
        Assert.Equal("alert", savedAlert.AlertType);
        Assert.Equal("Pulse", savedAlert.Indicator);
        Assert.Equal(patientId, savedAlert.PatientId);

        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync("md-alerts", It.IsAny<PatientAlertMessage>()), Times.Once);
        _redisServiceMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<DataAnalysisService.DTOs.LastAlertInfo>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_NonExistingPatient_StillProcessesAlert()
    {
        // Arrange
        var nonExistingPatientId = Guid.NewGuid();
        var metric = new MetricDto
        {
            PatientId = nonExistingPatientId,
            Type = "Pulse",
            Value = 150
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<DataAnalysisService.DTOs.LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync((DataAnalysisService.DTOs.LastAlertInfo?)null);
        _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<DataAnalysisService.DTOs.LastAlertInfo>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        _kafkaProducerServiceMock.Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<PatientAlertMessage>()))
            .Returns(Task.CompletedTask);

        // Act
        await _dataAnalysisService.AnalyzeMetricAsync(metric);

        // Assert
        var savedAlerts = await _context.Alerts.Where(a => a.PatientId == nonExistingPatientId).ToListAsync();
        Assert.Single(savedAlerts);
        
        var savedAlert = savedAlerts[0];
        Assert.Equal("alert", savedAlert.AlertType);
        Assert.Equal("Pulse", savedAlert.Indicator);

        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync("md-alerts", It.IsAny<PatientAlertMessage>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_MultipleMetricsForSamePatient_CreatesMultipleAlerts()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new PatientDto
        {
            PatientId = patientId,
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Петрович",
            Ward = 101
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var metrics = new[]
        {
            new MetricDto { PatientId = patientId, Type = "Pulse", Value = 150 },
            new MetricDto { PatientId = patientId, Type = "Temperature", Value = 38.5 },
            new MetricDto { PatientId = patientId, Type = "SystolicPressure", Value = 160 }
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<DataAnalysisService.DTOs.LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync((DataAnalysisService.DTOs.LastAlertInfo?)null);
        _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<DataAnalysisService.DTOs.LastAlertInfo>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        _kafkaProducerServiceMock.Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<PatientAlertMessage>()))
            .Returns(Task.CompletedTask);

        // Act
        foreach (var metric in metrics)
        {
            await _dataAnalysisService.AnalyzeMetricAsync(metric);
        }

        // Assert
        var savedAlerts = await _context.Alerts.Where(a => a.PatientId == patientId).ToListAsync();
        Assert.Equal(3, savedAlerts.Count);
        
        Assert.Contains(savedAlerts, a => a.Indicator == "Pulse");
        Assert.Contains(savedAlerts, a => a.Indicator == "Temperature");
        Assert.Contains(savedAlerts, a => a.Indicator == "SystolicPressure");

        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync("md-alerts", It.IsAny<PatientAlertMessage>()), Times.Exactly(3));
    }

    [Fact]
    public async Task AnalyzeMetricAsync_ValueWithinLimits_NoAlertCreated()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new PatientDto
        {
            PatientId = patientId,
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Петрович",
            Ward = 101
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 80 // В пределах лимитов
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(79.0); // Небольшое изменение
        _redisServiceMock.Setup(x => x.GetAsync<DataAnalysisService.DTOs.LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync((DataAnalysisService.DTOs.LastAlertInfo?)null);
        _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        // Act
        await _dataAnalysisService.AnalyzeMetricAsync(metric);

        // Assert
        var savedAlerts = await _context.Alerts.Where(a => a.PatientId == patientId).ToListAsync();
        Assert.Empty(savedAlerts);

        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<PatientAlertMessage>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_FirstMetricValue_NoAlertCreated()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new PatientDto
        {
            PatientId = patientId,
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Петрович",
            Ward = 101
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 80
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync((double?)null); // Первое значение
        _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        // Act
        await _dataAnalysisService.AnalyzeMetricAsync(metric);

        // Assert
        var savedAlerts = await _context.Alerts.Where(a => a.PatientId == patientId).ToListAsync();
        Assert.Empty(savedAlerts);

        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<PatientAlertMessage>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_AlertWithinTimeout_NoNewAlertCreated()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new PatientDto
        {
            PatientId = patientId,
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Петрович",
            Ward = 101
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 150
        };

        var lastAlert = new DataAnalysisService.DTOs.LastAlertInfo
        {
            AlertType = "alert",
            Timestamp = DateTime.UtcNow.AddMinutes(-2) // Меньше таймаута в 5 минут
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<DataAnalysisService.DTOs.LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync(lastAlert);
        _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        // Act
        await _dataAnalysisService.AnalyzeMetricAsync(metric);

        // Assert
        var savedAlerts = await _context.Alerts.Where(a => a.PatientId == patientId).ToListAsync();
        Assert.Empty(savedAlerts);

        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<PatientAlertMessage>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_AlertAfterTimeout_NewAlertCreated()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new PatientDto
        {
            PatientId = patientId,
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Петрович",
            Ward = 101
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 150
        };

        var lastAlert = new DataAnalysisService.DTOs.LastAlertInfo
        {
            AlertType = "alert",
            Timestamp = DateTime.UtcNow.AddMinutes(-10) // Больше таймаута в 5 минут
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<DataAnalysisService.DTOs.LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync(lastAlert);
        _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<DataAnalysisService.DTOs.LastAlertInfo>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        _kafkaProducerServiceMock.Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<PatientAlertMessage>()))
            .Returns(Task.CompletedTask);

        // Act
        await _dataAnalysisService.AnalyzeMetricAsync(metric);

        // Assert
        var savedAlerts = await _context.Alerts.Where(a => a.PatientId == patientId).ToListAsync();
        Assert.Single(savedAlerts);

        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync("md-alerts", It.IsAny<PatientAlertMessage>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_ValueChangeExceedsThreshold_AlertCreated()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new PatientDto
        {
            PatientId = patientId,
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Петрович",
            Ward = 101
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 86 // Изменение на < 5% от 80
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<DataAnalysisService.DTOs.LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync((DataAnalysisService.DTOs.LastAlertInfo?)null);
        _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);
        _redisServiceMock.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<DataAnalysisService.DTOs.LastAlertInfo>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        _kafkaProducerServiceMock.Setup(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<PatientAlertMessage>()))
            .Returns(Task.CompletedTask);

        // Act
        await _dataAnalysisService.AnalyzeMetricAsync(metric);

        // Assert
        var savedAlerts = await _context.Alerts.Where(a => a.PatientId == patientId).ToListAsync();
        Assert.Single(savedAlerts);
        
        var savedAlert = savedAlerts[0];
        Assert.Equal("alert", savedAlert.AlertType);
        Assert.Equal("Pulse", savedAlert.Indicator);

        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync("md-alerts", It.IsAny<PatientAlertMessage>()), Times.Once);
    }
} 