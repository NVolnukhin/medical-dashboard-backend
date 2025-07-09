using DataAnalysisService.Data;
using DataAnalysisService.Services.Patient;
using Microsoft.EntityFrameworkCore;
using Shared;
using Xunit;

namespace Tests.MedicalDashboard.DASTests.Unit.Services.Patient;

public class PatientServiceTests
{
    private readonly DbContextOptions<DataAnalysisDbContext> _options;
    private readonly DataAnalysisDbContext _context;
    private readonly PatientService _service;

    public PatientServiceTests()
    {
        _options = new DbContextOptionsBuilder<DataAnalysisDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataAnalysisDbContext(_options);
        _service = new PatientService(_context);
    }

    [Fact]
    public async Task GetPatientByIdAsync_ExistingPatient_ReturnsPatient()
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

        // Act
        var result = await _service.GetPatientByIdAsync(patientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(patientId, result!.PatientId);
        Assert.Equal("Иван", result.FirstName);
        Assert.Equal("Иванов", result.LastName);
        Assert.Equal("Петрович", result.MiddleName);
        Assert.Equal(101, result.Ward);
    }

    [Fact]
    public async Task GetPatientByIdAsync_NonExistingPatient_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _service.GetPatientByIdAsync(nonExistingId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPatientFullNameAsync_ExistingPatient_ReturnsFullName()
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

        // Act
        var result = await _service.GetPatientFullNameAsync(patientId);

        // Assert
        Assert.Equal("Иван Петрович Иванов из палаты №101", result);
    }

    [Fact]
    public async Task GetPatientFullNameAsync_PatientWithoutMiddleName_ReturnsNameWithoutMiddleName()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new PatientDto
        {
            PatientId = patientId,
            FirstName = "Петр",
            LastName = "Петров",
            MiddleName = null,
            Ward = 102
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPatientFullNameAsync(patientId);

        // Assert
        Assert.Equal("Петр Петров из палаты №102", result);
    }

    [Fact]
    public async Task GetPatientFullNameAsync_PatientWithEmptyMiddleName_ReturnsNameWithoutMiddleName()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new PatientDto
        {
            PatientId = patientId,
            FirstName = "Анна",
            LastName = "Сидорова",
            MiddleName = "",
            Ward = 103
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPatientFullNameAsync(patientId);

        // Assert
        Assert.Equal("Анна Сидорова из палаты №103", result);
    }

    [Fact]
    public async Task GetPatientFullNameAsync_NonExistingPatient_ReturnsEmptyString()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _service.GetPatientFullNameAsync(nonExistingId);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GetPatientByIdAsync_MultiplePatients_ReturnsCorrectPatient()
    {
        // Arrange
        var patient1Id = Guid.NewGuid();
        var patient2Id = Guid.NewGuid();

        var patient1 = new PatientDto
        {
            PatientId = patient1Id,
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Петрович",
            Ward = 101
        };

        var patient2 = new PatientDto
        {
            PatientId = patient2Id,
            FirstName = "Петр",
            LastName = "Петров",
            MiddleName = "Иванович",
            Ward = 102
        };

        _context.Patients.AddRange(patient1, patient2);
        await _context.SaveChangesAsync();

        // Act
        var result1 = await _service.GetPatientByIdAsync(patient1Id);
        var result2 = await _service.GetPatientByIdAsync(patient2Id);

        // Assert
        Assert.NotNull(result1);
        Assert.Equal(patient1Id, result1!.PatientId);
        Assert.Equal("Иван", result1.FirstName);

        Assert.NotNull(result2);
        Assert.Equal(patient2Id, result2!.PatientId);
        Assert.Equal("Петр", result2.FirstName);
    }

    [Fact]
    public async Task GetPatientFullNameAsync_PatientWithWhitespaceMiddleName_ReturnsNameWithoutMiddleName()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new PatientDto
        {
            PatientId = patientId,
            FirstName = "Мария",
            LastName = "Козлова",
            MiddleName = "",
            Ward = 104
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPatientFullNameAsync(patientId);

        // Assert
        Assert.Equal("Мария Козлова из палаты №104", result);
    }

    [Fact]
    public async Task GetPatientByIdAsync_EmptyDatabase_ReturnsNull()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        // Act
        var result = await _service.GetPatientByIdAsync(patientId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPatientFullNameAsync_EmptyDatabase_ReturnsEmptyString()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        // Act
        var result = await _service.GetPatientFullNameAsync(patientId);

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task GetPatientByIdAsync_PatientWithSpecialCharacters_ReturnsCorrectly()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new PatientDto
        {
            PatientId = patientId,
            FirstName = "Жан-Пьер",
            LastName = "О'Коннор",
            MiddleName = "Мария-Луиза",
            Ward = 105
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPatientByIdAsync(patientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(patientId, result!.PatientId);
        Assert.Equal("Жан-Пьер", result.FirstName);
        Assert.Equal("О'Коннор", result.LastName);
        Assert.Equal("Мария-Луиза", result.MiddleName);
    }

    [Fact]
    public async Task GetPatientFullNameAsync_PatientWithSpecialCharacters_ReturnsCorrectFullName()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new PatientDto
        {
            PatientId = patientId,
            FirstName = "Жан-Пьер",
            LastName = "О'Коннор",
            MiddleName = "Мария-Луиза",
            Ward = 105
        };

        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPatientFullNameAsync(patientId);

        // Assert
        Assert.Equal("Жан-Пьер Мария-Луиза О'Коннор из палаты №105", result);
    }
} 