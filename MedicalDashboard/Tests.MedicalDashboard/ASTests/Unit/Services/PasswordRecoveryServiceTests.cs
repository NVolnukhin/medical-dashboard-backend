using AuthService.DTOs.PasswordRecovery;
using AuthService.Kafka;
using AuthService.Models;
using AuthService.Repository.PasswordRecovery;
using AuthService.Repository.User;
using AuthService.Services.PasswordRecovery;
using AuthService.Services.RecoveryToken;
using AuthService.Services.User;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.MedicalDashboard.ASTests.Unit.Services;

public class PasswordRecoveryServiceTests
{
    private readonly Mock<IPasswordRecoveryTokenRepository> _tokenRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ILogger<PasswordRecoveryService>> _loggerMock = new();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock = new();
    private readonly Mock<IKafkaProducerService> _notificationServiceMock = new();
    private readonly PasswordRecoveryService _service;

    public PasswordRecoveryServiceTests()
    {
        _service = new PasswordRecoveryService(
            _tokenRepositoryMock.Object,
            _userRepositoryMock.Object,
            _loggerMock.Object,
            _userServiceMock.Object,
            _tokenGeneratorMock.Object,
            _notificationServiceMock.Object
        );
    }

    [Fact]
    public async Task RequestRecoveryAsync_ReturnsSuccess_WhenUserExists()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Андрей",
            LastName = "Петров",
            PhoneNumber = "+777777777",
            Password = "password",
            Role = "Doctor"
        };
        var token = "generatedToken";

        _userRepositoryMock.Setup(r => r.GetByEmail(email))
            .ReturnsAsync(user);

        _tokenGeneratorMock.Setup(g => g.GenerateToken())
            .Returns(token);

        _tokenRepositoryMock.Setup(r => r.InvalidateUserTokensAsync(user.Id))
            .Returns(Task.CompletedTask);

        _tokenRepositoryMock.Setup(r => r.CreateTokenAsync(It.IsAny<PasswordRecoveryToken>()))
            .Returns(Task.CompletedTask);

        _notificationServiceMock.Setup(n => n.SendNotificationAsync(It.IsAny<NotificationMessage>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RequestRecoveryAsync(email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Success);
        Assert.Equal("Если аккаунт с таким email существует, письмо отправлено", result.Value.Message);

        _tokenRepositoryMock.Verify(r => r.InvalidateUserTokensAsync(user.Id), Times.Once);
        _tokenRepositoryMock.Verify(r => r.CreateTokenAsync(It.Is<PasswordRecoveryToken>(t => 
            t.UserId == user.Id && 
            t.Token == token && 
            !t.IsUsed)), Times.Once);
        _notificationServiceMock.Verify(n => n.SendNotificationAsync(It.Is<NotificationMessage>(m =>
            m.Recipient == email &&
            m.Subject == "Change password request")), Times.Once);
    }

    [Fact]
    public async Task RequestRecoveryAsync_ReturnsSuccess_WhenUserDoesNotExist()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _userRepositoryMock.Setup(r => r.GetByEmail(email))
            .ReturnsAsync((User)null);

        // Act
        var result = await _service.RequestRecoveryAsync(email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Success);
        Assert.Equal("Если аккаунт с таким email существует, инструкции будут отправлены", result.Value.Message);

        _tokenRepositoryMock.Verify(r => r.InvalidateUserTokensAsync(It.IsAny<Guid>()), Times.Never);
        _tokenRepositoryMock.Verify(r => r.CreateTokenAsync(It.IsAny<PasswordRecoveryToken>()), Times.Never);
        _notificationServiceMock.Verify(n => n.SendNotificationAsync(It.IsAny<NotificationMessage>()), Times.Never);
    }

    [Fact]
    public async Task RequestRecoveryAsync_ReturnsFailure_WhenNotificationFails()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = "Андрей",
            LastName = "Петров",
            PhoneNumber = "+777777777",
            Password = "password"
        };

        _userRepositoryMock.Setup(r => r.GetByEmail(email))
            .ReturnsAsync(user);

        _tokenGeneratorMock.Setup(g => g.GenerateToken())
            .Returns("token");

        _tokenRepositoryMock.Setup(r => r.InvalidateUserTokensAsync(user.Id))
            .Returns(Task.CompletedTask);

        _tokenRepositoryMock.Setup(r => r.CreateTokenAsync(It.IsAny<PasswordRecoveryToken>()))
            .Returns(Task.CompletedTask);

        _notificationServiceMock.Setup(n => n.SendNotificationAsync(It.IsAny<NotificationMessage>()))
            .ThrowsAsync(new Exception("Notification failed"));

        // Act
        var result = await _service.RequestRecoveryAsync(email);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Ошибка при отправке письма с инструкциями", result.Errors[0].Message);
    }

    [Fact]
    public async Task ConfirmRecoveryAsync_ReturnsSuccess_WhenTokenIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "validToken";
        var newPassword = "newPassword123";
        var confirmPassword = "newPassword123";

        var recoveryToken = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Андрей",
            LastName = "Петров",
            PhoneNumber = "+777777777",
            Password = "password",
            Role = "Doctor"
        };

        var request = new PasswordRecoveryConfirm(token, newPassword, confirmPassword);

        _tokenRepositoryMock.Setup(r => r.GetValidTokenAsync(token))
            .ReturnsAsync(recoveryToken);

        _userServiceMock.Setup(s => s.RecoverPassword(userId, newPassword, confirmPassword))
            .ReturnsAsync(Result.Ok());

        _tokenRepositoryMock.Setup(r => r.CreateTokenAsync(It.IsAny<PasswordRecoveryToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock.Setup(r => r.GetById(userId))
            .ReturnsAsync(user);

        _notificationServiceMock.Setup(n => n.SendNotificationAsync(It.IsAny<NotificationMessage>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ConfirmRecoveryAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Success);
        Assert.Equal("Пароль успешно изменен", result.Value.Message);

        _tokenRepositoryMock.Verify(r => r.CreateTokenAsync(It.Is<PasswordRecoveryToken>(t => 
            t.Id == recoveryToken.Id && 
            t.IsUsed)), Times.Once);
        _notificationServiceMock.Verify(n => n.SendNotificationAsync(It.Is<NotificationMessage>(m =>
            m.Recipient == user.Email &&
            m.Subject == "Password recovered")), Times.Once);
    }


    [Fact]
    public async Task ConfirmRecoveryAsync_ReturnsFailure_WhenTokenIsInvalid()
    {
        // Arrange
        var request = new PasswordRecoveryConfirm("invalidToken", "newPassword", "newPassword");

        _tokenRepositoryMock.Setup(r => r.GetValidTokenAsync(request.Token))
            .ReturnsAsync((PasswordRecoveryToken)null);

        // Act
        var result = await _service.ConfirmRecoveryAsync(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Недействительная или просроченная ссылка", result.Errors[0].Message);

        _userServiceMock.Verify(s => s.RecoverPassword(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _tokenRepositoryMock.Verify(r => r.CreateTokenAsync(It.IsAny<PasswordRecoveryToken>()), Times.Never);
        _notificationServiceMock.Verify(n => n.SendNotificationAsync(It.IsAny<NotificationMessage>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmRecoveryAsync_ReturnsFailure_WhenPasswordRecoveryFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "validToken";
        var newPassword = "newPassword123";
        var confirmPassword = "newPassword123";

        var recoveryToken = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        var request = new PasswordRecoveryConfirm(token, newPassword, confirmPassword);

        _tokenRepositoryMock.Setup(r => r.GetValidTokenAsync(token))
            .ReturnsAsync(recoveryToken);

        _userServiceMock.Setup(s => s.RecoverPassword(userId, newPassword, confirmPassword))
            .ReturnsAsync(Result.Fail("Пароли не совпадают"));

        // Act
        var result = await _service.ConfirmRecoveryAsync(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Пароли не совпадают", result.Errors[0].Message);

        _tokenRepositoryMock.Verify(r => r.CreateTokenAsync(It.IsAny<PasswordRecoveryToken>()), Times.Never);
        _notificationServiceMock.Verify(n => n.SendNotificationAsync(It.IsAny<NotificationMessage>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmRecoveryAsync_ReturnsFailure_WhenNotificationFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "validToken";
        var newPassword = "newPassword123";
        var confirmPassword = "newPassword123";

        var recoveryToken = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Андрей",
            LastName = "Петров",
            PhoneNumber = "+777777777",
            Password = "password"
        };

        var request = new PasswordRecoveryConfirm(token, newPassword, confirmPassword);

        _tokenRepositoryMock.Setup(r => r.GetValidTokenAsync(token))
            .ReturnsAsync(recoveryToken);

        _userServiceMock.Setup(s => s.RecoverPassword(userId, newPassword, confirmPassword))
            .ReturnsAsync(Result.Ok());

        _tokenRepositoryMock.Setup(r => r.CreateTokenAsync(It.IsAny<PasswordRecoveryToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock.Setup(r => r.GetById(userId))
            .ReturnsAsync(user);

        _notificationServiceMock.Setup(n => n.SendNotificationAsync(It.IsAny<NotificationMessage>()))
            .ThrowsAsync(new Exception("Notification failed"));

        // Act
        var result = await _service.ConfirmRecoveryAsync(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Ошибка при восстановлении пароля", result.Errors[0].Message);
    }
} 