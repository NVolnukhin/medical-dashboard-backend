using DataAnalysisService.Data;
using Microsoft.EntityFrameworkCore;
using Shared;
using Xunit;

namespace Tests.MedicalDashboard.DASTests.Integration.Database;

public class DataAnalysisDbContextTests
{
    private readonly DbContextOptions<DataAnalysisDbContext> _options;
    private readonly DataAnalysisDbContext _context;

    public DataAnalysisDbContextTests()
    {
        _options = new DbContextOptionsBuilder<DataAnalysisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataAnalysisDbContext(_options);
    }

    [Fact]
    public async Task Patients_CanAddAndRetrievePatient()
    {
        // Arrange
        var patient = new PatientDto
        {
            PatientId = Guid.NewGuid(),
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Петрович",
            Ward = 101
        };

        // Act
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var retrievedPatient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == patient.PatientId);

        // Assert
        Assert.NotNull(retrievedPatient);
        Assert.Equal(patient.PatientId, retrievedPatient!.PatientId);
        Assert.Equal(patient.FirstName, retrievedPatient.FirstName);
        Assert.Equal(patient.LastName, retrievedPatient.LastName);
        Assert.Equal(patient.MiddleName, retrievedPatient.MiddleName);
        Assert.Equal(patient.Ward, retrievedPatient.Ward);
    }

    [Fact]
    public async Task Patients_CanUpdatePatient()
    {
        // Arrange
        var patient = new PatientDto
        {
            PatientId = Guid.NewGuid(),
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Петрович",
            Ward = 101
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        // Act
        patient.FirstName = "Петр";
        patient.Ward = 102;
        await _context.SaveChangesAsync();

        var updatedPatient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == patient.PatientId);

        // Assert
        Assert.NotNull(updatedPatient);
        Assert.Equal("Петр", updatedPatient!.FirstName);
        Assert.Equal(102, updatedPatient.Ward);
    }

    [Fact]
    public async Task Patients_CanDeletePatient()
    {
        // Arrange
        var patient = new PatientDto
        {
            PatientId = Guid.NewGuid(),
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Петрович",
            Ward = 101
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        // Act
        _context.Patients.Remove(patient);
        await _context.SaveChangesAsync();

        var deletedPatient = await _context.Patients.FirstOrDefaultAsync(p => p.PatientId == patient.PatientId);

        // Assert
        Assert.Null(deletedPatient);
    }

    [Fact]
    public async Task Alerts_CanAddAndRetrieveAlert()
    {
        // Arrange
        var alert = new AlertDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            AlertType = "alert",
            Indicator = "Pulse",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();

        var retrievedAlert = await _context.Alerts.FirstOrDefaultAsync(a => a.Id == alert.Id);

        // Assert
        Assert.NotNull(retrievedAlert);
        Assert.Equal(alert.Id, retrievedAlert!.Id);
        Assert.Equal(alert.PatientId, retrievedAlert.PatientId);
        Assert.Equal(alert.AlertType, retrievedAlert.AlertType);
        Assert.Equal(alert.Indicator, retrievedAlert.Indicator);
        Assert.Equal(alert.CreatedAt, retrievedAlert.CreatedAt);
    }

    [Fact]
    public async Task Alerts_CanUpdateAlert()
    {
        // Arrange
        var alert = new AlertDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            AlertType = "alert",
            Indicator = "Pulse",
            CreatedAt = DateTime.UtcNow
        };

        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();

        // Act
        alert.AlertType = "warning";
        alert.Indicator = "Temperature";
        await _context.SaveChangesAsync();

        var updatedAlert = await _context.Alerts.FirstOrDefaultAsync(a => a.Id == alert.Id);

        // Assert
        Assert.NotNull(updatedAlert);
        Assert.Equal("warning", updatedAlert!.AlertType);
        Assert.Equal("Temperature", updatedAlert.Indicator);
    }

    [Fact]
    public async Task Alerts_CanDeleteAlert()
    {
        // Arrange
        var alert = new AlertDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            AlertType = "alert",
            Indicator = "Pulse",
            CreatedAt = DateTime.UtcNow
        };

        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();

        // Act
        _context.Alerts.Remove(alert);
        await _context.SaveChangesAsync();

        var deletedAlert = await _context.Alerts.FirstOrDefaultAsync(a => a.Id == alert.Id);

        // Assert
        Assert.Null(deletedAlert);
    }

    [Fact]
    public async Task Patients_CanQueryMultiplePatients()
    {
        // Arrange
        var patients = new List<PatientDto>
        {
            new() { PatientId = Guid.NewGuid(), FirstName = "Иван", LastName = "Иванов", MiddleName = "Петрович", Ward = 101 },
            new() { PatientId = Guid.NewGuid(), FirstName = "Петр", LastName = "Петров", MiddleName = "Иванович", Ward = 102 },
            new() { PatientId = Guid.NewGuid(), FirstName = "Анна", LastName = "Сидорова", MiddleName = "Сергеевна", Ward = 103 }
        };

        _context.Patients.AddRange(patients);
        await _context.SaveChangesAsync();

        // Act
        var retrievedPatients = await _context.Patients.ToListAsync();

        // Assert
        Assert.Equal(3, retrievedPatients.Count);
        Assert.Contains(retrievedPatients, p => p.FirstName == "Иван");
        Assert.Contains(retrievedPatients, p => p.FirstName == "Петр");
        Assert.Contains(retrievedPatients, p => p.FirstName == "Анна");
    }

    [Fact]
    public async Task Alerts_CanQueryMultipleAlerts()
    {
        // Arrange
        var alerts = new List<AlertDto>
        {
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "alert", Indicator = "Pulse", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "warning", Indicator = "Temperature", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "info", Indicator = "SystolicPressure", CreatedAt = DateTime.UtcNow }
        };

        _context.Alerts.AddRange(alerts);
        await _context.SaveChangesAsync();

        // Act
        var retrievedAlerts = await _context.Alerts.ToListAsync();

        // Assert
        Assert.Equal(3, retrievedAlerts.Count);
        Assert.Contains(retrievedAlerts, a => a.AlertType == "alert");
        Assert.Contains(retrievedAlerts, a => a.AlertType == "warning");
        Assert.Contains(retrievedAlerts, a => a.AlertType == "info");
    }

    [Fact]
    public async Task Patients_CanFilterPatientsByWard()
    {
        // Arrange
        var patients = new List<PatientDto>
        {
            new() { PatientId = Guid.NewGuid(), FirstName = "Иван", LastName = "Иванов", MiddleName = "Петрович", Ward = 101 },
            new() { PatientId = Guid.NewGuid(), FirstName = "Петр", LastName = "Петров", MiddleName = "Иванович", Ward = 101 },
            new() { PatientId = Guid.NewGuid(), FirstName = "Анна", LastName = "Сидорова", MiddleName = "Сергеевна", Ward = 102 }
        };

        _context.Patients.AddRange(patients);
        await _context.SaveChangesAsync();

        // Act
        var ward101Patients = await _context.Patients.Where(p => p.Ward == 101).ToListAsync();

        // Assert
        Assert.Equal(2, ward101Patients.Count);
        Assert.All(ward101Patients, p => Assert.Equal(101, p.Ward));
    }

    [Fact]
    public async Task Alerts_CanFilterAlertsByType()
    {
        // Arrange
        var alerts = new List<AlertDto>
        {
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "alert", Indicator = "Pulse", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "alert", Indicator = "Temperature", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "warning", Indicator = "SystolicPressure", CreatedAt = DateTime.UtcNow }
        };

        _context.Alerts.AddRange(alerts);
        await _context.SaveChangesAsync();

        // Act
        var alertTypeAlerts = await _context.Alerts.Where(a => a.AlertType == "alert").ToListAsync();

        // Assert
        Assert.Equal(2, alertTypeAlerts.Count);
        Assert.All(alertTypeAlerts, a => Assert.Equal("alert", a.AlertType));
    }

    [Fact]
    public async Task Alerts_CanFilterAlertsByPatientId()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var alerts = new List<AlertDto>
        {
            new() { Id = Guid.NewGuid(), PatientId = patientId, AlertType = "alert", Indicator = "Pulse", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PatientId = patientId, AlertType = "warning", Indicator = "Temperature", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "alert", Indicator = "SystolicPressure", CreatedAt = DateTime.UtcNow }
        };

        _context.Alerts.AddRange(alerts);
        await _context.SaveChangesAsync();

        // Act
        var patientAlerts = await _context.Alerts.Where(a => a.PatientId == patientId).ToListAsync();

        // Assert
        Assert.Equal(2, patientAlerts.Count);
        Assert.All(patientAlerts, a => Assert.Equal(patientId, a.PatientId));
    }

    [Fact]
    public async Task Patients_CanOrderPatientsByFirstName()
    {
        // Arrange
        var patients = new List<PatientDto>
        {
            new() { PatientId = Guid.NewGuid(), FirstName = "Петр", LastName = "Петров", MiddleName = "Иванович", Ward = 101 },
            new() { PatientId = Guid.NewGuid(), FirstName = "Анна", LastName = "Сидорова", MiddleName = "Сергеевна", Ward = 102 },
            new() { PatientId = Guid.NewGuid(), FirstName = "Иван", LastName = "Иванов", MiddleName = "Петрович", Ward = 103 }
        };

        _context.Patients.AddRange(patients);
        await _context.SaveChangesAsync();

        // Act
        var orderedPatients = await _context.Patients.OrderBy(p => p.FirstName).ToListAsync();

        // Assert
        Assert.Equal(3, orderedPatients.Count);
        Assert.Equal("Анна", orderedPatients[0].FirstName);
        Assert.Equal("Иван", orderedPatients[1].FirstName);
        Assert.Equal("Петр", orderedPatients[2].FirstName);
    }

    [Fact]
    public async Task Alerts_CanOrderAlertsByCreatedAt()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var alerts = new List<AlertDto>
        {
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "alert", Indicator = "Pulse", CreatedAt = now.AddHours(2) },
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "warning", Indicator = "Temperature", CreatedAt = now },
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "info", Indicator = "SystolicPressure", CreatedAt = now.AddHours(1) }
        };

        _context.Alerts.AddRange(alerts);
        await _context.SaveChangesAsync();

        // Act
        var orderedAlerts = await _context.Alerts.OrderBy(a => a.CreatedAt).ToListAsync();

        // Assert
        Assert.Equal(3, orderedAlerts.Count);
        Assert.Equal(now, orderedAlerts[0].CreatedAt);
        Assert.Equal(now.AddHours(1), orderedAlerts[1].CreatedAt);
        Assert.Equal(now.AddHours(2), orderedAlerts[2].CreatedAt);
    }

    [Fact]
    public async Task Patients_CanCountPatients()
    {
        // Arrange
        var patients = new List<PatientDto>
        {
            new() { PatientId = Guid.NewGuid(), FirstName = "Иван", LastName = "Иванов", MiddleName = "Петрович", Ward = 101 },
            new() { PatientId = Guid.NewGuid(), FirstName = "Петр", LastName = "Петров", MiddleName = "Иванович", Ward = 102 },
            new() { PatientId = Guid.NewGuid(), FirstName = "Анна", LastName = "Сидорова", MiddleName = "Сергеевна", Ward = 103 }
        };

        _context.Patients.AddRange(patients);
        await _context.SaveChangesAsync();

        // Act
        var count = await _context.Patients.CountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Alerts_CanCountAlerts()
    {
        // Arrange
        var alerts = new List<AlertDto>
        {
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "alert", Indicator = "Pulse", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "warning", Indicator = "Temperature", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), PatientId = Guid.NewGuid(), AlertType = "info", Indicator = "SystolicPressure", CreatedAt = DateTime.UtcNow }
        };

        _context.Alerts.AddRange(alerts);
        await _context.SaveChangesAsync();

        // Act
        var count = await _context.Alerts.CountAsync();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Patients_CanCheckIfPatientExists()
    {
        // Arrange
        var patient = new PatientDto
        {
            PatientId = Guid.NewGuid(),
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Петрович",
            Ward = 101
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _context.Patients.AnyAsync(p => p.PatientId == patient.PatientId);
        var nonExists = await _context.Patients.AnyAsync(p => p.PatientId == Guid.NewGuid());

        // Assert
        Assert.True(exists);
        Assert.False(nonExists);
    }

    [Fact]
    public async Task Alerts_CanCheckIfAlertExists()
    {
        // Arrange
        var alert = new AlertDto
        {
            Id = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            AlertType = "alert",
            Indicator = "Pulse",
            CreatedAt = DateTime.UtcNow
        };

        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _context.Alerts.AnyAsync(a => a.Id == alert.Id);
        var nonExists = await _context.Alerts.AnyAsync(a => a.Id == Guid.NewGuid());

        // Assert
        Assert.True(exists);
        Assert.False(nonExists);
    }
} 