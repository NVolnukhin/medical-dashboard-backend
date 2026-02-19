using DataAnalysisService.Config;
using DataAnalysisService.DTOs;
using DataAnalysisService.Services.Alert;
using DataAnalysisService.Services.Kafka.Producer;
using DataAnalysisService.Services.Patient;
using DataAnalysisService.Services.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shared;
using Shared.MetricLimits;
using Xunit;

namespace Tests.MedicalDashboard.DASTests.Unit.Services.Analysis;

public class DataAnalysisServiceTests
{
    private readonly Mock<IRedisService> _redisServiceMock;
    private readonly Mock<IPatientService> _patientServiceMock;
    private readonly Mock<IKafkaProducerService> _kafkaProducerServiceMock;
    private readonly Mock<IAlertService> _alertServiceMock;
    private readonly Mock<ILogger<DataAnalysisService.Services.Analysis.DataAnalysisService>> _loggerMock;
    private readonly AnalysisSettings _analysisSettings;
    private readonly MetricsConfig _metricsConfig;
    private readonly DataAnalysisService.Services.Analysis.DataAnalysisService _service;

    public DataAnalysisServiceTests()
    {
        _redisServiceMock = new Mock<IRedisService>();
        _patientServiceMock = new Mock<IPatientService>();
        _kafkaProducerServiceMock = new Mock<IKafkaProducerService>();
        _alertServiceMock = new Mock<IAlertService>();
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

        _service = new DataAnalysisService.Services.Analysis.DataAnalysisService(
            _redisServiceMock.Object,
            _patientServiceMock.Object,
            _kafkaProducerServiceMock.Object,
            _alertServiceMock.Object,
            analysisSettingsOptions,
            metricsConfigOptions,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task AnalyzeMetricAsync_UnknownMetricType_LogsWarningAndReturns()
    {
        // Arrange
        var metric = new MetricDto
        {
            PatientId = Guid.NewGuid(),
            Type = "UnknownType",
            Value = 100
        };

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        // Для неизвестного типа метрики метод должен только логировать предупреждение и возвращаться
        _redisServiceMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<double>(), It.IsAny<TimeSpan>()), Times.Never);
        _redisServiceMock.Verify(x => x.GetAsync<double?>(It.IsAny<string>()), Times.Never);
        // Не должно быть вызовов анализа
        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        _alertServiceMock.Verify(x => x.CreateAlertAsync(It.IsAny<AlertDto>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_FirstMetricValue_LogsInfoAndReturns()
    {
        // Arrange
        var metric = new MetricDto
        {
            PatientId = Guid.NewGuid(),
            Type = "Pulse",
            Value = 80
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync((double?)null);

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        _redisServiceMock.Verify(x => x.SetAsync(It.IsAny<string>(), metric.Value, It.IsAny<TimeSpan>()), Times.Once);
        _redisServiceMock.Verify(x => x.GetAsync<double?>(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_ValueOutsideLimits_SendsAlert()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 150 // Вне лимитов [60, 100]
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync((LastAlertInfo?)null);
        _patientServiceMock.Setup(x => x.GetPatientFullNameAsync(patientId))
            .ReturnsAsync("Иван Иванов из палаты №101");

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync("md-alerts", It.IsAny<PatientAlertMessage>()), Times.Once);
        _alertServiceMock.Verify(x => x.CreateAlertAsync(It.IsAny<AlertDto>()), Times.Once);
        _redisServiceMock.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<LastAlertInfo>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_ValueChangeExceedsAlertThreshold_SendsAlert()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 84 // Изменение на 5% от 80
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync((LastAlertInfo?)null);
        _patientServiceMock.Setup(x => x.GetPatientFullNameAsync(patientId))
            .ReturnsAsync("Иван Иванов из палаты №101");

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync("md-alerts", It.IsAny<PatientAlertMessage>()), Times.Once);
        _alertServiceMock.Verify(x => x.CreateAlertAsync(It.IsAny<AlertDto>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_ValueCloseToLimits_SendsWarning()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 62 // Близко к нижнему лимиту (60 + 3% от диапазона)
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync((LastAlertInfo?)null);
        _patientServiceMock.Setup(x => x.GetPatientFullNameAsync(patientId))
            .ReturnsAsync("Иван Иванов из палаты №101");

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync("md-alerts", It.IsAny<PatientAlertMessage>()), Times.Once);
        _alertServiceMock.Verify(x => x.CreateAlertAsync(It.IsAny<AlertDto>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_ValueChangeExceedsWarningThreshold_SendsWarning()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 82.4 // Изменение на 3% от 80
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync((LastAlertInfo?)null);
        _patientServiceMock.Setup(x => x.GetPatientFullNameAsync(patientId))
            .ReturnsAsync("Иван Иванов из палаты №101");

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync("md-alerts", It.IsAny<PatientAlertMessage>()), Times.Once);
        _alertServiceMock.Verify(x => x.CreateAlertAsync(It.IsAny<AlertDto>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_LastAlertWithinTimeout_DoesNotSendAlert()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 150
        };

        var lastAlert = new LastAlertInfo
        {
            AlertType = "alert",
            Timestamp = DateTime.UtcNow.AddMinutes(-2) // Меньше таймаута в 5 минут
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync(lastAlert);

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        _alertServiceMock.Verify(x => x.CreateAlertAsync(It.IsAny<AlertDto>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_LastAlertAfterTimeout_SendsAlert()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 150
        };

        var lastAlert = new LastAlertInfo
        {
            AlertType = "alert",
            Timestamp = DateTime.UtcNow.AddMinutes(-10) // Больше таймаута в 5 минут
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync(lastAlert);
        _patientServiceMock.Setup(x => x.GetPatientFullNameAsync(patientId))
            .ReturnsAsync("Иван Иванов из палаты №101");

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync("md-alerts", It.IsAny<PatientAlertMessage>()), Times.Once);
        _alertServiceMock.Verify(x => x.CreateAlertAsync(It.IsAny<AlertDto>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_LastWarningNowAlert_SendsAlert()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 150
        };

        var lastAlert = new LastAlertInfo
        {
            AlertType = "warning",
            Timestamp = DateTime.UtcNow.AddMinutes(-1)
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync(lastAlert);
        _patientServiceMock.Setup(x => x.GetPatientFullNameAsync(patientId))
            .ReturnsAsync("Иван Иванов из палаты №101");

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync("md-alerts", It.IsAny<PatientAlertMessage>()), Times.Once);
        _alertServiceMock.Verify(x => x.CreateAlertAsync(It.IsAny<AlertDto>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_LastAlertNowWarning_DoesNotSendWarning()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 62 // Близко к лимиту, но не за пределами
        };

        var lastAlert = new LastAlertInfo
        {
            AlertType = "alert",
            Timestamp = DateTime.UtcNow.AddMinutes(-1)
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync(lastAlert);

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        _alertServiceMock.Verify(x => x.CreateAlertAsync(It.IsAny<AlertDto>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_ExceptionOccurs_LogsFailure()
    {
        // Arrange
        var metric = new MetricDto
        {
            PatientId = Guid.NewGuid(),
            Type = "Pulse",
            Value = 80
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test exception"));

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        // Проверяем, что исключение было обработано и не проброшено дальше
        Assert.True(true); // Если мы дошли сюда, значит исключение было обработано
    }

    [Theory]
    [InlineData("Pulse", 60, 100)]
    [InlineData("Temperature", 36.0, 37.5)]
    [InlineData("SystolicPressure", 90, 140)]
    [InlineData("DiastolicPressure", 60, 90)]
    [InlineData("RespirationRate", 12, 20)]
    [InlineData("Saturation", 95, 100)]
    [InlineData("Weight", 40, 150)]
    [InlineData("Hemoglobin", 120, 160)]
    [InlineData("Cholesterol", 3.0, 5.2)]
    [InlineData("BMI", 18.5, 25.0)]
    public async Task AnalyzeMetricAsync_DifferentMetricTypes_UseCorrectLimits(string metricType, double min, double max)
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = metricType,
            Value = max + 10 // За пределами лимитов
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(min);
        _redisServiceMock.Setup(x => x.GetAsync<LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync((LastAlertInfo?)null);
        _patientServiceMock.Setup(x => x.GetPatientFullNameAsync(patientId))
            .ReturnsAsync("Иван Иванов из палаты №101");

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync("md-alerts", It.IsAny<PatientAlertMessage>()), Times.Once);
        _alertServiceMock.Verify(x => x.CreateAlertAsync(It.IsAny<AlertDto>()), Times.Once);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_ValueWithinLimitsAndNoSignificantChange_NoAlerts()
    {
        // Arrange
        var metric = new MetricDto
        {
            PatientId = Guid.NewGuid(),
            Type = "Pulse",
            Value = 80 // В пределах лимитов
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(79.0); // Небольшое изменение
        _redisServiceMock.Setup(x => x.GetAsync<LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync((LastAlertInfo?)null);

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        _alertServiceMock.Verify(x => x.CreateAlertAsync(It.IsAny<AlertDto>()), Times.Never);
    }

    [Fact]
    public async Task AnalyzeMetricAsync_AlertAndWarningThresholds_RespectPriority()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var metric = new MetricDto
        {
            PatientId = patientId,
            Type = "Pulse",
            Value = 150 // За пределами лимитов (должен быть alert)
        };

        _redisServiceMock.Setup(x => x.GetAsync<double?>(It.IsAny<string>()))
            .ReturnsAsync(80.0);
        _redisServiceMock.Setup(x => x.GetAsync<LastAlertInfo>(It.IsAny<string>()))
            .ReturnsAsync((LastAlertInfo?)null);
        _patientServiceMock.Setup(x => x.GetPatientFullNameAsync(patientId))
            .ReturnsAsync("Иван Иванов из палаты №101");

        // Act
        await _service.AnalyzeMetricAsync(metric);

        // Assert
        // Должен быть отправлен только alert, а не warning
        _kafkaProducerServiceMock.Verify(x => x.ProduceAsync("md-alerts", It.Is<PatientAlertMessage>(m => m.AlertType == "alert")), Times.Once);
        _alertServiceMock.Verify(x => x.CreateAlertAsync(It.Is<AlertDto>(a => a.AlertType == "alert")), Times.Once);
    }
} 