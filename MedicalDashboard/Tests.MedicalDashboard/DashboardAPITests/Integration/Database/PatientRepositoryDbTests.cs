using DashboardAPI.Data;
using DashboardAPI.Repositories.Patient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.MedicalDashboard.DashboardAPITests.Integration.Database;

public class PatientRepositoryDbTests : IDisposable
{
    private readonly DbContextOptions<DashboardDbContext> _options;
    private readonly DashboardDbContext _context;
    private readonly PatientRepository _repository;
    private readonly Mock<ILogger<PatientRepository>> _loggerMock;

    public PatientRepositoryDbTests()
    {
        _options = new DbContextOptionsBuilder<DashboardDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DashboardDbContext(_options);
        _loggerMock = new Mock<ILogger<PatientRepository>>();
        _repository = new PatientRepository(_context);

        // Создаем тестовые данные
        SeedTestData();
    }

    private void SeedTestData()
    {
        var patients = new List<DashboardAPI.Models.Patient>
        {
            new()
            {
                PatientId = Guid.NewGuid(),
                FirstName = "Иван",
                LastName = "Иванов",
                MiddleName = "Иванович",
                DoctorId = Guid.NewGuid(),
                BirthDate = new DateTime(1990, 1, 1),
                Sex = 'M',
                Height = 180,
                Ward = 101
            },
            new()
            {
                PatientId = Guid.NewGuid(),
                FirstName = "Петр",
                LastName = "Петров",
                MiddleName = "Петрович",
                DoctorId = Guid.NewGuid(),
                BirthDate = new DateTime(1985, 5, 15),
                Sex = 'M',
                Height = 175,
                Ward = 102
            },
            new()
            {
                PatientId = Guid.NewGuid(),
                FirstName = "Анна",
                LastName = "Сидорова",
                MiddleName = "Сидоровна",
                DoctorId = Guid.NewGuid(),
                BirthDate = new DateTime(1992, 8, 20),
                Sex = 'F',
                Height = 165,
                Ward = 101
            }
        };

        _context.Patients.AddRange(patients);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllPatients_WhenNoFilters()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        var patients = result.ToList();
        Assert.Equal(3, patients.Count);
    }

    [Fact]
    public async Task GetAllAsync_WithNameFilter_ReturnsFilteredPatients()
    {
        // Act
        var result = await _repository.GetAllAsync("Петр");

        // Assert
        var patients = result.ToList();
        Assert.Single(patients);
        Assert.Equal("Петр", patients[0].FirstName);
    }

    [Fact]
    public async Task GetAllAsync_WithWardFilter_ReturnsFilteredPatients()
    {
        // Act
        var result = await _repository.GetAllAsync(ward: 101);

        // Assert
        var patients = result.ToList();
        Assert.Equal(2, patients.Count);
        Assert.All(patients, p => Assert.Equal(101, p.Ward));
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsCorrectPage()
    {
        // Act
        var result = await _repository.GetAllAsync(page: 1, pageSize: 2);

        // Assert
        var patients = result.ToList();
        Assert.Equal(2, patients.Count);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPatient_WhenPatientExists()
    {
        // Arrange
        var existingPatient = _context.Patients.First();

        // Act
        var result = await _repository.GetByIdAsync(existingPatient.PatientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingPatient.PatientId, result.PatientId);
        Assert.Equal(existingPatient.FirstName, result.FirstName);
        Assert.Equal(existingPatient.LastName, result.LastName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenPatientDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_CreatesNewPatient_WhenValidData()
    {
        // Arrange
        var newPatient = new DashboardAPI.Models.Patient
        {
            FirstName = "Новый",
            LastName = "Пациент",
            MiddleName = "Новый",
            DoctorId = Guid.NewGuid(),
            BirthDate = new DateTime(1995, 3, 10),
            Sex = 'M',
            Height = 170,
            Ward = 103
        };

        // Act
        var result = await _repository.CreateAsync(newPatient);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.PatientId);
        Assert.Equal(newPatient.FirstName, result.FirstName);
        Assert.Equal(newPatient.LastName, result.LastName);

        // Verify it was saved to database
        var savedPatient = await _context.Patients.FindAsync(result.PatientId);
        Assert.NotNull(savedPatient);
        Assert.Equal(newPatient.FirstName, savedPatient.FirstName);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesExistingPatient_WhenValidData()
    {
        // Arrange
        var existingPatient = _context.Patients.First();
        var originalName = existingPatient.FirstName;
        existingPatient.FirstName = "ОбновленноеИмя";

        // Act
        var result = await _repository.UpdateAsync(existingPatient);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ОбновленноеИмя", result.FirstName);
        // Assert.Equal(originalName, existingPatient.FirstName); // Оригинальный объект изменён, это ожидаемо

        // Verify it was updated in database
        var updatedPatient = await _context.Patients.FindAsync(existingPatient.PatientId);
        Assert.NotNull(updatedPatient); 
        Assert.Equal("ОбновленноеИмя", updatedPatient.FirstName);
    }

    [Fact]
    public async Task DeleteAsync_DeletesPatient_WhenPatientExists()
    {
        // Arrange
        var existingPatient = _context.Patients.First();
        var patientId = existingPatient.PatientId;

        // Act
        await _repository.DeleteAsync(patientId);

        // Assert
        var deletedPatient = await _context.Patients.FindAsync(patientId);
        Assert.Null(deletedPatient);
    }

    [Fact]
    public async Task GetTotalCountAsync_ReturnsCorrectCount_WhenNoFilters()
    {
        // Act
        var result = await _repository.GetTotalCountAsync();

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task GetTotalCountAsync_WithNameFilter_ReturnsFilteredCount()
    {
        // Act
        var result = await _repository.GetTotalCountAsync("Иван");

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task GetTotalCountAsync_WithWardFilter_ReturnsFilteredCount()
    {
        // Act
        var result = await _repository.GetTotalCountAsync(ward: 101);

        // Assert
        Assert.Equal(2, result);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
} 