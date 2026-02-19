using DashboardAPI.Data;
using DashboardAPI.Models;
using DashboardAPI.Repositories.Patient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Repositories;

public class PatientRepositoryTests
{
    private readonly DbContextOptions<DashboardDbContext> _options;

    public PatientRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<DashboardDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetAllAsync_WithNoFilters_ShouldReturnAllPatients()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new PatientRepository(context);

        var patients = new List<Patient>
        {
            new Patient
            {
                PatientId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                MiddleName = "Smith",
                DoctorId = Guid.NewGuid(),
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Sex = 'M'
            },
            new Patient
            {
                PatientId = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith",
                MiddleName = "Johnson",
                DoctorId = Guid.NewGuid(),
                BirthDate = DateTime.UtcNow.AddYears(-25),
                Sex = 'F'
            }
        };

        context.Patients.AddRange(patients);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_WithWardFilter_ShouldReturnFilteredPatients()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new PatientRepository(context);

        var patients = new List<Patient>
        {
            new Patient
            {
                PatientId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                MiddleName = "Smith",
                DoctorId = Guid.NewGuid(),
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Sex = 'M',
                Ward = 101
            },
            new Patient
            {
                PatientId = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith",
                MiddleName = "Johnson",
                DoctorId = Guid.NewGuid(),
                BirthDate = DateTime.UtcNow.AddYears(-25),
                Sex = 'F',
                Ward = 102
            }
        };

        context.Patients.AddRange(patients);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(ward: 101);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(result, p => p.Ward == 101);
    }

    [Fact]
    public async Task GetAllAsync_WithDoctorIdFilter_ShouldReturnFilteredPatients()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new PatientRepository(context);

        var doctorId = Guid.NewGuid();
        var patients = new List<Patient>
        {
            new Patient
            {
                PatientId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                MiddleName = "Smith",
                DoctorId = doctorId,
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Sex = 'M'
            },
            new Patient
            {
                PatientId = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith",
                MiddleName = "Johnson",
                DoctorId = Guid.NewGuid(),
                BirthDate = DateTime.UtcNow.AddYears(-25),
                Sex = 'F'
            }
        };

        context.Patients.AddRange(patients);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(doctorId: doctorId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains(result, p => p.DoctorId == doctorId);
    }

    [Fact]
    public async Task GetAllAsync_WithPagination_ShouldReturnPaginatedResults()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new PatientRepository(context);

        var patients = new List<Patient>
        {
            new Patient
            {
                PatientId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                MiddleName = "Smith",
                DoctorId = Guid.NewGuid(),
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Sex = 'M'
            },
            new Patient
            {
                PatientId = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith",
                MiddleName = "Johnson",
                DoctorId = Guid.NewGuid(),
                BirthDate = DateTime.UtcNow.AddYears(-25),
                Sex = 'F'
            },
            new Patient
            {
                PatientId = Guid.NewGuid(),
                FirstName = "Bob",
                LastName = "Johnson",
                MiddleName = "Wilson",
                DoctorId = Guid.NewGuid(),
                BirthDate = DateTime.UtcNow.AddYears(-35),
                Sex = 'M'
            }
        };

        context.Patients.AddRange(patients);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetAllAsync(page: 1, pageSize: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnPatient()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new PatientRepository(context);

        var patient = new Patient
        {
            PatientId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            MiddleName = "Smith",
            DoctorId = Guid.NewGuid(),
            BirthDate = DateTime.UtcNow.AddYears(-30),
            Sex = 'M'
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByIdAsync(patient.PatientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(patient.PatientId, result.PatientId);
        Assert.Equal(patient.FirstName, result.FirstName);
        Assert.Equal(patient.LastName, result.LastName);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new PatientRepository(context);

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithValidPatient_ShouldSaveToDatabase()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new PatientRepository(context);

        var patient = new Patient
        {
            PatientId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            MiddleName = "Smith",
            DoctorId = Guid.NewGuid(),
            BirthDate = DateTime.UtcNow.AddYears(-30),
            Sex = 'M'
        };

        // Act
        var result = await repository.CreateAsync(patient);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(patient.PatientId, result.PatientId);
        Assert.Equal(patient.FirstName, result.FirstName);

        var savedPatient = await context.Patients.FirstOrDefaultAsync(p => p.PatientId == patient.PatientId);
        Assert.NotNull(savedPatient);
    }

    [Fact]
    public async Task CreateAsync_WithNullPatient_ShouldThrowNullReferenceException()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new PatientRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            repository.CreateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_WithValidPatient_ShouldUpdatePatient()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new PatientRepository(context);

        var patient = new Patient
        {
            PatientId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            MiddleName = "Smith",
            DoctorId = Guid.NewGuid(),
            BirthDate = DateTime.UtcNow.AddYears(-30),
            Sex = 'M'
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        patient.FirstName = "Updated John";
        patient.LastName = "Updated Doe";

        // Act
        var result = await repository.UpdateAsync(patient);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated John", result.FirstName);
        Assert.Equal("Updated Doe", result.LastName);

        var updatedPatient = await context.Patients.FirstOrDefaultAsync(p => p.PatientId == patient.PatientId);
        Assert.NotNull(updatedPatient);
        Assert.Equal("Updated John", updatedPatient.FirstName);
    }

    [Fact]
    public async Task UpdateAsync_WithNullPatient_ShouldThrowNullReferenceException()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new PatientRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_WithValidId_ShouldRemovePatient()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new PatientRepository(context);

        var patient = new Patient
        {
            PatientId = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            MiddleName = "Smith",
            DoctorId = Guid.NewGuid(),
            BirthDate = DateTime.UtcNow.AddYears(-30),
            Sex = 'M'
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(patient.PatientId);

        // Assert
        var deletedPatient = await context.Patients.FirstOrDefaultAsync(p => p.PatientId == patient.PatientId);
        Assert.Null(deletedPatient);
    }

    [Fact]
    public async Task DeleteAsync_WithInvalidId_ShouldNotThrowException()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new PatientRepository(context);

        // Act & Assert
        await repository.DeleteAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task GetTotalCountAsync_WithNoFilters_ShouldReturnTotalCount()
    {
        // Arrange
        using var context = new DashboardDbContext(_options);
        var repository = new PatientRepository(context);

        var patients = new List<Patient>
        {
            new Patient
            {
                PatientId = Guid.NewGuid(),
                FirstName = "John",
                LastName = "Doe",
                MiddleName = "Smith",
                DoctorId = Guid.NewGuid(),
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Sex = 'M'
            },
            new Patient
            {
                PatientId = Guid.NewGuid(),
                FirstName = "Jane",
                LastName = "Smith",
                MiddleName = "Johnson",
                DoctorId = Guid.NewGuid(),
                BirthDate = DateTime.UtcNow.AddYears(-25),
                Sex = 'F'
            }
        };

        context.Patients.AddRange(patients);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetTotalCountAsync();

        // Assert
        Assert.Equal(2, result);
    }
}