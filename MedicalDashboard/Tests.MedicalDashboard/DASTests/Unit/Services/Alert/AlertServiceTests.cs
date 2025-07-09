using DataAnalysisService.Data;
using DataAnalysisService.Services.Alert;
using Microsoft.EntityFrameworkCore;
using Shared;
using Xunit;

namespace Tests.MedicalDashboard.DASTests.Unit.Services.Alert;

public class AlertServiceTests
{
    private readonly DbContextOptions<DataAnalysisDbContext> _options;
    private readonly DataAnalysisDbContext _context;
    private readonly AlertService _service;

    public AlertServiceTests()
    {
        _options = new DbContextOptionsBuilder<DataAnalysisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataAnalysisDbContext(_options);
        _service = new AlertService(_context);
    }

    [Fact]
    public async Task CreateAlertAsync_ValidAlert_CreatesSuccessfully()
    {
        // Arrange
        var alertDto = new AlertDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            AlertType = "alert",
            Indicator = "Pulse",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _service.CreateAlertAsync(alertDto);

        // Assert
        var savedAlert = await _context.Alerts.FirstOrDefaultAsync(a => a.Id == alertDto.Id);
        Assert.NotNull(savedAlert);
        Assert.Equal(alertDto.PatientId, savedAlert.PatientId);
        Assert.Equal(alertDto.AlertType, savedAlert.AlertType);
        Assert.Equal(alertDto.Indicator, savedAlert.Indicator);
        Assert.Equal(alertDto.CreatedAt, savedAlert.CreatedAt);
    }

    [Fact]
    public async Task CreateAlertAsync_MultipleAlerts_CreatesAllSuccessfully()
    {
        // Arrange
        var alert1 = new AlertDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            AlertType = "alert",
            Indicator = "Pulse",
            CreatedAt = DateTime.UtcNow
        };

        var alert2 = new AlertDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            AlertType = "warning",
            Indicator = "Temperature",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _service.CreateAlertAsync(alert1);
        await _service.CreateAlertAsync(alert2);

        // Assert
        var savedAlerts = await _context.Alerts.ToListAsync();
        Assert.Equal(2, savedAlerts.Count);
        
        var savedAlert1 = savedAlerts.FirstOrDefault(a => a.Id == alert1.Id);
        var savedAlert2 = savedAlerts.FirstOrDefault(a => a.Id == alert2.Id);
        
        Assert.NotNull(savedAlert1);
        Assert.NotNull(savedAlert2);
        Assert.Equal("alert", savedAlert1!.AlertType);
        Assert.Equal("warning", savedAlert2!.AlertType);
    }

    [Fact]
    public async Task CreateAlertAsync_AlertWithAllProperties_CreatesWithAllProperties()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var alertId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var alertDto = new AlertDto
        {
            Id = alertId,
            PatientId = patientId,
            AlertType = "alert",
            Indicator = "SystolicPressure",
            CreatedAt = createdAt
        };

        // Act
        await _service.CreateAlertAsync(alertDto);

        // Assert
        var savedAlert = await _context.Alerts.FirstOrDefaultAsync(a => a.Id == alertId);
        Assert.NotNull(savedAlert);
        Assert.Equal(alertId, savedAlert!.Id);
        Assert.Equal(patientId, savedAlert.PatientId);
        Assert.Equal("alert", savedAlert.AlertType);
        Assert.Equal("SystolicPressure", savedAlert.Indicator);
        Assert.Equal(createdAt, savedAlert.CreatedAt);
    }

    [Fact]
    public async Task CreateAlertAsync_DifferentAlertTypes_CreatesCorrectly()
    {
        // Arrange
        var alertTypes = new[] { "alert", "warning", "info" };
        var alerts = new List<AlertDto>();

        foreach (var alertType in alertTypes)
        {
            alerts.Add(new AlertDto
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                AlertType = alertType,
                Indicator = "TestIndicator",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Act
        foreach (var alert in alerts)
        {
            await _service.CreateAlertAsync(alert);
        }

        // Assert
        var savedAlerts = await _context.Alerts.ToListAsync();
        Assert.Equal(3, savedAlerts.Count);
        
        foreach (var alertType in alertTypes)
        {
            Assert.Contains(savedAlerts, a => a.AlertType == alertType);
        }
    }

    [Fact]
    public async Task CreateAlertAsync_DifferentIndicators_CreatesCorrectly()
    {
        // Arrange
        var indicators = new[] { "Pulse", "Temperature", "SystolicPressure", "DiastolicPressure", "RespirationRate", "Saturation", "Weight", "Hemoglobin", "Cholesterol", "BMI" };
        var alerts = new List<AlertDto>();

        foreach (var indicator in indicators)
        {
            alerts.Add(new AlertDto
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                AlertType = "alert",
                Indicator = indicator,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Act
        foreach (var alert in alerts)
        {
            await _service.CreateAlertAsync(alert);
        }

        // Assert
        var savedAlerts = await _context.Alerts.ToListAsync();
        Assert.Equal(10, savedAlerts.Count);
        
        foreach (var indicator in indicators)
        {
            Assert.Contains(savedAlerts, a => a.Indicator == indicator);
        }
    }

    [Fact]
    public async Task CreateAlertAsync_SamePatientMultipleAlerts_CreatesAllSuccessfully()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var alerts = new List<AlertDto>();

        for (int i = 0; i < 5; i++)
        {
            alerts.Add(new AlertDto
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                AlertType = i % 2 == 0 ? "alert" : "warning",
                Indicator = $"Indicator{i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }

        // Act
        foreach (var alert in alerts)
        {
            await _service.CreateAlertAsync(alert);
        }

        // Assert
        var savedAlerts = await _context.Alerts.Where(a => a.PatientId == patientId).ToListAsync();
        Assert.Equal(5, savedAlerts.Count);
        
        for (int i = 0; i < 5; i++)
        {
            var expectedAlert = alerts[i];
            var savedAlert = savedAlerts.FirstOrDefault(a => a.Id == expectedAlert.Id);
            Assert.NotNull(savedAlert);
            Assert.Equal(expectedAlert.AlertType, savedAlert!.AlertType);
            Assert.Equal(expectedAlert.Indicator, savedAlert.Indicator);
        }
    }

    [Fact]
    public async Task CreateAlertAsync_AlertWithSpecialCharacters_CreatesCorrectly()
    {
        // Arrange
        var alertDto = new AlertDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            AlertType = "alert",
            Indicator = "Pulse (High)",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _service.CreateAlertAsync(alertDto);

        // Assert
        var savedAlert = await _context.Alerts.FirstOrDefaultAsync(a => a.Id == alertDto.Id);
        Assert.NotNull(savedAlert);
        Assert.Equal("Pulse (High)", savedAlert!.Indicator);
    }

    [Fact]
    public async Task CreateAlertAsync_AlertWithLongIndicator_CreatesCorrectly()
    {
        // Arrange
        var longIndicator = new string('A', 1000);
        var alertDto = new AlertDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            AlertType = "alert",
            Indicator = longIndicator,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _service.CreateAlertAsync(alertDto);

        // Assert
        var savedAlert = await _context.Alerts.FirstOrDefaultAsync(a => a.Id == alertDto.Id);
        Assert.NotNull(savedAlert);
        Assert.Equal(longIndicator, savedAlert!.Indicator);
    }

    [Fact]
    public async Task CreateAlertAsync_AlertWithFutureDate_CreatesCorrectly()
    {
        // Arrange
        var futureDate = DateTime.UtcNow.AddDays(1);
        var alertDto = new AlertDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            AlertType = "alert",
            Indicator = "Pulse",
            CreatedAt = futureDate
        };

        // Act
        await _service.CreateAlertAsync(alertDto);

        // Assert
        var savedAlert = await _context.Alerts.FirstOrDefaultAsync(a => a.Id == alertDto.Id);
        Assert.NotNull(savedAlert);
        Assert.Equal(futureDate, savedAlert!.CreatedAt);
    }

    [Fact]
    public async Task CreateAlertAsync_AlertWithPastDate_CreatesCorrectly()
    {
        // Arrange
        var pastDate = DateTime.UtcNow.AddDays(-1);
        var alertDto = new AlertDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            AlertType = "alert",
            Indicator = "Pulse",
            CreatedAt = pastDate
        };

        // Act
        await _service.CreateAlertAsync(alertDto);

        // Assert
        var savedAlert = await _context.Alerts.FirstOrDefaultAsync(a => a.Id == alertDto.Id);
        Assert.NotNull(savedAlert);
        Assert.Equal(pastDate, savedAlert!.CreatedAt);
    }

    [Fact]
    public async Task CreateAlertAsync_EmptyDatabase_CreatesFirstAlert()
    {
        // Arrange
        var alertDto = new AlertDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            AlertType = "alert",
            Indicator = "Pulse",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _service.CreateAlertAsync(alertDto);

        // Assert
        var savedAlerts = await _context.Alerts.ToListAsync();
        Assert.Single(savedAlerts);
        Assert.Equal(alertDto.Id, savedAlerts[0].Id);
    }

    [Fact]
    public async Task CreateAlertAsync_ConcurrentAlerts_CreatesAllSuccessfully()
    {
        // Arrange
        var tasks = new List<Task>();
        var alerts = new List<AlertDto>();

        for (int i = 0; i < 10; i++)
        {
            var alert = new AlertDto
            {
                Id = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                AlertType = "alert",
                Indicator = $"Indicator{i}",
                CreatedAt = DateTime.UtcNow
            };
            alerts.Add(alert);
            tasks.Add(_service.CreateAlertAsync(alert));
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert
        var savedAlerts = await _context.Alerts.ToListAsync();
        Assert.Equal(10, savedAlerts.Count);
        
        foreach (var alert in alerts)
        {
            Assert.Contains(savedAlerts, a => a.Id == alert.Id);
        }
    }
} 