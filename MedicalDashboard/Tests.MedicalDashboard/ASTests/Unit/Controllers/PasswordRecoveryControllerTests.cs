using AuthService.Controllers;
using AuthService.DTOs.PasswordRecovery;
using AuthService.Services.PasswordRecovery;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.MedicalDashboard.ASTests.Unit.Controllers;

public class PasswordRecoveryControllerTests
{
    private readonly Mock<IPasswordRecoveryService> _recoveryServiceMock = new();
    private readonly Mock<ILogger<PasswordRecoveryController>> _loggerMock = new();
    private readonly PasswordRecoveryController _controller;

    public PasswordRecoveryControllerTests()
    {
        _controller = new PasswordRecoveryController(
            _recoveryServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task RequestRecovery_ReturnsOk_WhenUserExists()
    {
        // Arrange
        var request = new PasswordRecoveryRequest("test@example.com");
        var response = new PasswordRecoveryResponse(true, "Если аккаунт с таким email существует, письмо отправлено");

        _recoveryServiceMock.Setup(s => s.RequestRecoveryAsync(request.Email))
            .ReturnsAsync(Result.Ok(response));

        // Act
        var result = await _controller.RequestRecovery(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseValue = Assert.IsType<PasswordRecoveryResponse>(okResult.Value);
        Assert.True(responseValue.Success);
        Assert.Equal("Если аккаунт с таким email существует, письмо отправлено", responseValue.Message);
    }

    [Fact]
    public async Task RequestRecovery_ReturnsBadRequest_WhenServiceFails()
    {
        // Arrange
        var request = new PasswordRecoveryRequest("test@example.com");

        _recoveryServiceMock.Setup(s => s.RequestRecoveryAsync(request.Email))
            .ReturnsAsync(Result.Fail<PasswordRecoveryResponse>("Ошибка при запросе восстановления пароля"));

        // Act
        var result = await _controller.RequestRecovery(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Ошибка при запросе восстановления пароля", badRequestResult.Value);
    }

    [Fact]
    public async Task ConfirmRecovery_ReturnsOk_WhenConfirmationIsValid()
    {
        // Arrange
        var request = new PasswordRecoveryConfirm(
            "validToken",
            "newPassword",
            "newPassword"
        );
        var response = new PasswordRecoveryResponse(true, "Пароль успешно изменен");

        _recoveryServiceMock.Setup(s => s.ConfirmRecoveryAsync(request))
            .ReturnsAsync(Result.Ok(response));

        // Act
        var result = await _controller.ConfirmRecovery(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var responseValue = Assert.IsType<PasswordRecoveryResponse>(okResult.Value);
        Assert.True(responseValue.Success);
        Assert.Equal("Пароль успешно изменен", responseValue.Message);
    }

    [Fact]
    public async Task ConfirmRecovery_ReturnsBadRequest_WhenConfirmationIsInvalid()
    {
        // Arrange
        var request = new PasswordRecoveryConfirm(
            "invalidToken",
            "newPassword",
            "newPassword"
        );

        _recoveryServiceMock.Setup(s => s.ConfirmRecoveryAsync(request))
            .ReturnsAsync(Result.Fail<PasswordRecoveryResponse>("Недействительная или просроченная ссылка"));

        // Act
        var result = await _controller.ConfirmRecovery(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Ошибка при подтверждении восстановления", badRequestResult.Value);
    }
} 