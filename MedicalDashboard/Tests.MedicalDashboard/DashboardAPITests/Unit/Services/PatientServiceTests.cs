using DashboardAPI.DTOs;
using DashboardAPI.Repositories.Patient;
using DashboardAPI.Services.Patient;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Services;

public class PatientServiceTests
{
    private readonly Mock<IPatientRepository> _patientRepositoryMock = new();
    private readonly Mock<ILogger<PatientService>> _loggerMock = new();
    private readonly PatientService _service;

    public PatientServiceTests()
    {
        _service = new PatientService(_patientRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPatients_WhenPatientsExist()
    {
        // Arrange
        var patients = new List<DashboardAPI.Models.Patient>
        {
            new() { PatientId = Guid.NewGuid(), FirstName = "Иван", LastName = "Иванов", Ward = 101 },
            new() { PatientId = Guid.NewGuid(), FirstName = "Петр", LastName = "Петров", Ward = 102 }
        };

        _patientRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(patients);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        var patientDtos = result.ToList();
        Assert.Equal(2, patientDtos.Count);
        Assert.Equal(patients[0].PatientId, patientDtos[0].PatientId);
        Assert.Equal(patients[0].FirstName, patientDtos[0].FirstName);
        Assert.Equal(patients[0].LastName, patientDtos[0].LastName);
    }

    [Fact]
    public async Task GetAllAsync_WithFilters_ReturnsFilteredPatients()
    {
        // Arrange
        var patients = new List<DashboardAPI.Models.Patient>
        {
            new() { PatientId = Guid.NewGuid(), FirstName = "Иван", LastName = "Иванов", Ward = 101 }
        };

        _patientRepositoryMock.Setup(r => r.GetAllAsync("Иван", 101, It.IsAny<Guid?>(), 1, 20))
            .ReturnsAsync(patients);

        // Act
        var result = await _service.GetAllAsync("Иван", 101);

        // Assert
        var patientDtos = result.ToList();
        Assert.Single(patientDtos);
        Assert.Equal("Иван", patientDtos[0].FirstName);
        Assert.Equal(101, patientDtos[0].Ward);
    }

    [Fact]
    public async Task GetAllAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        _patientRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetAllAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsPatient_WhenPatientExists()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new DashboardAPI.Models.Patient { PatientId = patientId, FirstName = "Иван", LastName = "Иванов", Ward = 101 };

        _patientRepositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync(patient);

        // Act
        var result = await _service.GetByIdAsync(patientId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(patientId, result.PatientId);
        Assert.Equal("Иван", result.FirstName);
        Assert.Equal("Иванов", result.LastName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenPatientDoesNotExist()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _patientRepositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync((DashboardAPI.Models.Patient)null);

        // Act
        var result = await _service.GetByIdAsync(patientId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _patientRepositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetByIdAsync(patientId));
    }

    [Fact]
    public async Task CreateAsync_ReturnsCreatedPatient_WhenValidData()
    {
        // Arrange
        var apiDto = new ApiPatientDto
        {
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Иванович",
            DoctorId = Guid.NewGuid(),
            BirthDate = new DateTime(1990, 1, 1),
            Sex = 'M',
            Height = 180,
            Ward = 101
        };

        var createdPatient = new DashboardAPI.Models.Patient
        {
            PatientId = Guid.NewGuid(),
            FirstName = apiDto.FirstName,
            LastName = apiDto.LastName,
            MiddleName = apiDto.MiddleName,
            DoctorId = apiDto.DoctorId,
            BirthDate = apiDto.BirthDate,
            Sex = apiDto.Sex,
            Height = apiDto.Height,
            Ward = apiDto.Ward
        };

        _patientRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<DashboardAPI.Models.Patient>()))
            .ReturnsAsync(createdPatient);

        // Act
        var result = await _service.CreateAsync(apiDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdPatient.PatientId, result.PatientId);
        Assert.Equal(apiDto.FirstName, result.FirstName);
        Assert.Equal(apiDto.LastName, result.LastName);
        Assert.Equal(apiDto.MiddleName, result.MiddleName);
        Assert.Equal(apiDto.DoctorId, result.DoctorId);
        Assert.Equal(apiDto.BirthDate, result.BirthDate);
        Assert.Equal(apiDto.Sex, result.Sex);
        Assert.Equal(apiDto.Height, result.Height);
        Assert.Equal(apiDto.Ward, result.Ward);

        _patientRepositoryMock.Verify(r => r.CreateAsync(It.Is<DashboardAPI.Models.Patient>(p => 
            p.FirstName == apiDto.FirstName && 
            p.LastName == apiDto.LastName &&
            p.MiddleName == apiDto.MiddleName &&
            p.DoctorId == apiDto.DoctorId &&
            p.BirthDate == apiDto.BirthDate &&
            p.Sex == apiDto.Sex &&
            p.Height == apiDto.Height &&
            p.Ward == apiDto.Ward)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var apiDto = new ApiPatientDto { FirstName = "Иван", LastName = "Иванов" };

        _patientRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<DashboardAPI.Models.Patient>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.CreateAsync(apiDto));
    }

    [Fact]
    public async Task UpdateAsync_ReturnsUpdatedPatient_WhenPatientExists()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var existingPatient = new DashboardAPI.Models.Patient
        {
            PatientId = patientId,
            FirstName = "Иван",
            LastName = "Иванов",
            Ward = 101
        };

        var updateDto = new ApiPatientDto
        {
            FirstName = "Петр",
            LastName = "Петров",
            MiddleName = "Петрович",
            DoctorId = Guid.NewGuid(),
            BirthDate = new DateTime(1985, 5, 15),
            Sex = 'M',
            Height = 175,
            Ward = 102
        };

        var updatedPatient = new DashboardAPI.Models.Patient
        {
            PatientId = patientId,
            FirstName = updateDto.FirstName,
            LastName = updateDto.LastName,
            MiddleName = updateDto.MiddleName,
            DoctorId = updateDto.DoctorId,
            BirthDate = updateDto.BirthDate,
            Sex = updateDto.Sex,
            Height = updateDto.Height,
            Ward = updateDto.Ward
        };

        _patientRepositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync(existingPatient);
        _patientRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<DashboardAPI.Models.Patient>()))
            .ReturnsAsync(updatedPatient);

        // Act
        var result = await _service.UpdateAsync(patientId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(patientId, result.PatientId);
        Assert.Equal(updateDto.FirstName, result.FirstName);
        Assert.Equal(updateDto.LastName, result.LastName);
        Assert.Equal(updateDto.MiddleName, result.MiddleName);
        Assert.Equal(updateDto.DoctorId, result.DoctorId);
        Assert.Equal(updateDto.BirthDate, result.BirthDate);
        Assert.Equal(updateDto.Sex, result.Sex);
        Assert.Equal(updateDto.Height, result.Height);
        Assert.Equal(updateDto.Ward, result.Ward);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsArgumentException_WhenPatientDoesNotExist()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var updateDto = new ApiPatientDto { FirstName = "Петр", LastName = "Петров" };

        _patientRepositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync((DashboardAPI.Models.Patient)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(patientId, updateDto));
        Assert.Equal($"Пациент с ID {patientId} не найден", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var existingPatient = new DashboardAPI.Models.Patient { PatientId = patientId, FirstName = "Иван", LastName = "Иванов" };
        var updateDto = new ApiPatientDto { FirstName = "Петр", LastName = "Петров" };

        _patientRepositoryMock.Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync(existingPatient);
        _patientRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<DashboardAPI.Models.Patient>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.UpdateAsync(patientId, updateDto));
    }

    [Fact]
    public async Task DeleteAsync_CompletesSuccessfully_WhenPatientExists()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _patientRepositoryMock.Setup(r => r.DeleteAsync(patientId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(patientId);

        // Assert
        _patientRepositoryMock.Verify(r => r.DeleteAsync(patientId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _patientRepositoryMock.Setup(r => r.DeleteAsync(patientId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.DeleteAsync(patientId));
    }

    [Fact]
    public async Task GetTotalCountAsync_ReturnsCount_WhenValid()
    {
        // Arrange
        var expectedCount = 42;

        _patientRepositoryMock.Setup(r => r.GetTotalCountAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<Guid?>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _service.GetTotalCountAsync();

        // Assert
        Assert.Equal(expectedCount, result);
    }

    [Fact]
    public async Task GetTotalCountAsync_WithFilters_ReturnsFilteredCount()
    {
        // Arrange
        var expectedCount = 5;

        _patientRepositoryMock.Setup(r => r.GetTotalCountAsync("Иван", 101, It.IsAny<Guid?>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _service.GetTotalCountAsync("Иван", 101);

        // Assert
        Assert.Equal(expectedCount, result);
    }

    [Fact]
    public async Task GetTotalCountAsync_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        _patientRepositoryMock.Setup(r => r.GetTotalCountAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<Guid?>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetTotalCountAsync());
    }
} 