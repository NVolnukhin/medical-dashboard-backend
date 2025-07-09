using DashboardAPI.Controllers;
using DashboardAPI.DTOs;
using DashboardAPI.Services.Patient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Shared;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.MedicalDashboard.DashboardAPITests.Unit.Controllers;

public class PatientsControllerTests
{
    private readonly Mock<IPatientService> _patientServiceMock = new();
    private readonly Mock<ILogger<PatientsController>> _loggerMock = new();
    private readonly PatientsController _controller;

    public PatientsControllerTests()
    {
        _controller = new PatientsController(_patientServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetPatients_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _patientServiceMock.Setup(s => s.GetAllAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetPatients(null, null, null, 1, 20);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetPatient_ReturnsOk_WhenPatientExists()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new PatientDto { PatientId = patientId, FirstName = "Иван", LastName = "Иванов" };

        _patientServiceMock.Setup(s => s.GetByIdAsync(patientId))
            .ReturnsAsync(patient);

        // Act
        var result = await _controller.GetPatient(patientId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPatient = Assert.IsType<PatientDto>(okResult.Value);
        Assert.Equal(patientId, returnedPatient.PatientId);
    }

    [Fact]
    public async Task GetPatient_ReturnsNotFound_WhenPatientDoesNotExist()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _patientServiceMock.Setup(s => s.GetByIdAsync(patientId))
            .ReturnsAsync((PatientDto)null);

        // Act
        var result = await _controller.GetPatient(patientId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var value = notFoundResult.Value;
        var valueDict = value as IDictionary<string, object>;
        if (valueDict != null)
        {
            Assert.Equal("Пациент не найден", valueDict["error"]);
        }
        else
        {
            var errorProp = value.GetType().GetProperty("error");
            Assert.NotNull(errorProp);
            Assert.Equal("Пациент не найден", errorProp.GetValue(value)?.ToString());
        }
    }

    [Fact]
    public async Task GetPatient_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _patientServiceMock.Setup(s => s.GetByIdAsync(patientId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetPatient(patientId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task CreatePatient_ReturnsCreated_WhenPatientIsValid()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var apiDto = new ApiPatientDto { FirstName = "Иван", LastName = "Иванов" };
        var createdPatient = new PatientDto { PatientId = patientId, FirstName = "Иван", LastName = "Иванов" };

        _patientServiceMock.Setup(s => s.CreateAsync(apiDto))
            .ReturnsAsync(createdPatient);

        // Act
        var result = await _controller.CreatePatient(apiDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnedPatient = Assert.IsType<PatientDto>(createdResult.Value);
        Assert.Equal(patientId, returnedPatient.PatientId);
        Assert.Equal(nameof(PatientsController.GetPatient), createdResult.ActionName);
    }

    [Fact]
    public async Task CreatePatient_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        var apiDto = new ApiPatientDto { FirstName = "", LastName = "" };
        _controller.ModelState.AddModelError("FirstName", "Имя обязательно");

        // Act
        var result = await _controller.CreatePatient(apiDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreatePatient_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var apiDto = new ApiPatientDto { FirstName = "Иван", LastName = "Иванов" };

        _patientServiceMock.Setup(s => s.CreateAsync(apiDto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreatePatient(apiDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task UpdatePatient_ReturnsOk_WhenPatientExists()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var apiDto = new ApiPatientDto { FirstName = "Иван", LastName = "Иванов" };
        var updatedPatient = new PatientDto { PatientId = patientId, FirstName = "Иван", LastName = "Иванов" };

        _patientServiceMock.Setup(s => s.UpdateAsync(patientId, apiDto))
            .ReturnsAsync(updatedPatient);

        // Act
        var result = await _controller.UpdatePatient(patientId, apiDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedPatient = Assert.IsType<PatientDto>(okResult.Value);
        Assert.Equal(patientId, returnedPatient.PatientId);
    }

    [Fact]
    public async Task UpdatePatient_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var apiDto = new ApiPatientDto { FirstName = "", LastName = "" };
        _controller.ModelState.AddModelError("FirstName", "Имя обязательно");

        // Act
        var result = await _controller.UpdatePatient(patientId, apiDto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdatePatient_ReturnsNotFound_WhenPatientDoesNotExist()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var apiDto = new ApiPatientDto { FirstName = "Иван", LastName = "Иванов" };

        _patientServiceMock.Setup(s => s.UpdateAsync(patientId, apiDto))
            .ThrowsAsync(new ArgumentException("Пациент не найден"));

        // Act
        var result = await _controller.UpdatePatient(patientId, apiDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var value = notFoundResult.Value;
        var valueDict = value as IDictionary<string, object>;
        if (valueDict != null)
        {
            Assert.Equal("Пациент не найден", valueDict["error"]);
        }
        else
        {
            var errorProp = value.GetType().GetProperty("error");
            Assert.NotNull(errorProp);
            Assert.Equal("Пациент не найден", errorProp.GetValue(value)?.ToString());
        }
    }

    [Fact]
    public async Task UpdatePatient_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var apiDto = new ApiPatientDto { FirstName = "Иван", LastName = "Иванов" };

        _patientServiceMock.Setup(s => s.UpdateAsync(patientId, apiDto))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.UpdatePatient(patientId, apiDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task DeletePatient_ReturnsNoContent_WhenPatientExists()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _patientServiceMock.Setup(s => s.DeleteAsync(patientId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeletePatient(patientId);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeletePatient_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        var patientId = Guid.NewGuid();

        _patientServiceMock.Setup(s => s.DeleteAsync(patientId))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.DeletePatient(patientId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
} 