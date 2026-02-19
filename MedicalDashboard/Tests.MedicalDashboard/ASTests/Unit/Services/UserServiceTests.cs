using AuthService.DTOs;
using AuthService.Kafka;
using AuthService.Models;
using AuthService.Repository.PasswordRecovery;
using AuthService.Repository.User;
using AuthService.Services.Password;
using AuthService.Services.User;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.MedicalDashboard.ASTests.Unit.Services;

public class UserServiceTests
{
    private readonly Mock<ILogger<UserService>> _loggerMock = new();
    private readonly Mock<IPasswordRecoveryTokenRepository> _tokenRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordService> _passwordServiceMock = new();
    private readonly Mock<IKafkaProducerService> _notificationServiceMock = new();
    private readonly UserService _service;

    public UserServiceTests()
    {
        _service = new UserService(
            _loggerMock.Object,
            _tokenRepositoryMock.Object,
            _userRepositoryMock.Object,
            _passwordServiceMock.Object,
            _notificationServiceMock.Object
        );
    }

    [Fact]
    public async Task RecoverPassword_ReturnsSuccess_WhenAllValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newPassword = "newPassword123";
        var confirmPassword = "newPassword123";
        var newPasswordHash = "hashedPassword";
        var newSalt = "newSalt";

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            PhoneNumber = "+7777777777",
            Password = "oldPassword",
            Salt = "oldSalt"
        };

        _userRepositoryMock.Setup(r => r.GetById(userId))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(p => p.CreatePasswordHash(newPassword))
            .Returns((newPasswordHash, newSalt));

        _userRepositoryMock.Setup(r => r.UpdatePassword(userId, newPasswordHash, newSalt))
            .Returns(Task.CompletedTask);

        _tokenRepositoryMock.Setup(r => r.InvalidateUserTokensAsync(userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RecoverPassword(userId, newPassword, confirmPassword);

        // Assert
        Assert.True(result.IsSuccess);
        _userRepositoryMock.Verify(r => r.UpdatePassword(userId, newPasswordHash, newSalt), Times.Once);
        _tokenRepositoryMock.Verify(r => r.InvalidateUserTokensAsync(userId), Times.Once);
    }

    [Fact]
    public async Task RecoverPassword_ReturnsFailure_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newPassword = "newPassword123";
        var confirmPassword = "newPassword123";

        _userRepositoryMock.Setup(r => r.GetById(userId))
            .ReturnsAsync((User)null);

        // Act
        var result = await _service.RecoverPassword(userId, newPassword, confirmPassword);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Пользователь не найден", result.Errors[0].Message);
        _userRepositoryMock.Verify(r => r.UpdatePassword(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _tokenRepositoryMock.Verify(r => r.InvalidateUserTokensAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RecoverPassword_ReturnsFailure_WhenPasswordIsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newPassword = "";
        var confirmPassword = "";

        // Act
        var result = await _service.RecoverPassword(userId, newPassword, confirmPassword);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Новый пароль не может быть пустым", result.Errors[0].Message);
        _userRepositoryMock.Verify(r => r.UpdatePassword(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _tokenRepositoryMock.Verify(r => r.InvalidateUserTokensAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RecoverPassword_ReturnsFailure_WhenPasswordsDoNotMatch()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newPassword = "newPassword123";
        var confirmPassword = "differentPassword";

        // Act
        var result = await _service.RecoverPassword(userId, newPassword, confirmPassword);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Пароли не совпадают", result.Errors[0].Message);
        _userRepositoryMock.Verify(r => r.UpdatePassword(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _tokenRepositoryMock.Verify(r => r.InvalidateUserTokensAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RecoverPassword_ReturnsFailure_WhenUpdateFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var newPassword = "newPassword123";
        var confirmPassword = "newPassword123";

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            PhoneNumber = "+7777777777",
            Password = "oldPassword",
            Salt = "oldSalt"
        };

        _userRepositoryMock.Setup(r => r.GetById(userId))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(p => p.CreatePasswordHash(newPassword))
            .Returns(("hashedPassword", "newSalt"));

        _userRepositoryMock.Setup(r => r.UpdatePassword(userId, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.RecoverPassword(userId, newPassword, confirmPassword);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Произошла ошибка при восстановлении пароля", result.Errors[0].Message);
        _tokenRepositoryMock.Verify(r => r.InvalidateUserTokensAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePassword_ReturnsSuccess_WhenAllValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPassword = "oldPassword123";
        var newPassword = "newPassword123";
        var confirmPassword = "newPassword123";
        var oldPasswordHash = "hashedOldPassword";
        var newPasswordHash = "hashedNewPassword";
        var oldSalt = "oldSalt";
        var newSalt = "newSalt";

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            PhoneNumber = "+7777777777",
            Password = oldPasswordHash,
            Salt = oldSalt
        };

        var request = new UpdatePasswordRequest (
            oldPassword, 
            newPassword,
            confirmPassword);
            

        _userRepositoryMock.Setup(r => r.GetById(userId))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(p => p.ValidatePassword(oldPassword, oldSalt, oldPasswordHash))
            .Returns(true);

        _passwordServiceMock.Setup(p => p.CreatePasswordHash(newPassword))
            .Returns((newPasswordHash, newSalt));

        _userRepositoryMock.Setup(r => r.UpdatePassword(userId, newPasswordHash, newSalt))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdatePassword(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        _userRepositoryMock.Verify(r => r.UpdatePassword(userId, newPasswordHash, newSalt), Times.Once);
    }

    [Fact]
    public async Task UpdatePassword_ReturnsFailure_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdatePasswordRequest (
            "oldPassword", 
            "newPassword",
            "confirmPassword");

        _userRepositoryMock.Setup(r => r.GetById(userId))
            .ReturnsAsync((User)null);

        // Act
        var result = await _service.UpdatePassword(userId, request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Пользователь не найден", result.Errors[0].Message);
        _userRepositoryMock.Verify(r => r.UpdatePassword(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePassword_ReturnsFailure_WhenOldPasswordIsInvalid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPassword = "wrongPassword";
        var newPassword = "newPassword123";
        var confirmPassword = "newPassword123";
        var oldPasswordHash = "hashedOldPassword";
        var oldSalt = "oldSalt";

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            PhoneNumber = "+7777777777",
            Password = oldPasswordHash,
            Salt = oldSalt
        };

        var request = new UpdatePasswordRequest (
            oldPassword, 
            newPassword,
            confirmPassword);

        _userRepositoryMock.Setup(r => r.GetById(userId))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(p => p.ValidatePassword(oldPassword, oldSalt, oldPasswordHash))
            .Returns(false);

        // Act
        var result = await _service.UpdatePassword(userId, request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Неверный текущий пароль", result.Errors[0].Message);
        _userRepositoryMock.Verify(r => r.UpdatePassword(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePassword_ReturnsFailure_WhenNewPasswordIsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPassword = "oldPassword";
        var request = new UpdatePasswordRequest(
            oldPassword,
            "",
            "");

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            PhoneNumber = "+7777777777",
            Password = "hashedPassword",
            Salt = "salt"
        };

        _userRepositoryMock.Setup(r => r.GetById(userId))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(p => p.ValidatePassword(oldPassword, user.Salt, user.Password))
            .Returns(true);

        // Act
        var result = await _service.UpdatePassword(userId, request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Пароль не может быть пустым", result.Errors[0].Message);
        _userRepositoryMock.Verify(r => r.UpdatePassword(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task UpdatePassword_ReturnsFailure_WhenNewPasswordIsTooShort()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPassword = "oldPassword";
        var request = new UpdatePasswordRequest(
            oldPassword,
            "string",
            "string");

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            PhoneNumber = "+7777777777",
            Password = "hashedPassword",
            Salt = "salt"
        };

        _userRepositoryMock.Setup(r => r.GetById(userId))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(p => p.ValidatePassword(oldPassword, user.Salt, user.Password))
            .Returns(true);

        // Act
        var result = await _service.UpdatePassword(userId, request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Слишком короткий пароль", result.Errors[0].Message);
        _userRepositoryMock.Verify(r => r.UpdatePassword(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePassword_ReturnsFailure_WhenPasswordsDoNotMatch()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPassword = "oldPassword";
        var request = new UpdatePasswordRequest(
            oldPassword,
            "newPassword",
            "differentPassword");

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            PhoneNumber = "+7777777777",
            Password = "hashedPassword",
            Salt = "salt"
        };

        _userRepositoryMock.Setup(r => r.GetById(userId))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(p => p.ValidatePassword(oldPassword, user.Salt, user.Password))
            .Returns(true);

        // Act
        var result = await _service.UpdatePassword(userId, request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Пароли не совпадают", result.Errors[0].Message);
        _userRepositoryMock.Verify(r => r.UpdatePassword(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePassword_ReturnsFailure_WhenUpdateFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldPassword = "oldPassword123";
        var newPassword = "newPassword123";
        var confirmPassword = "newPassword123";
        var oldPasswordHash = "hashedOldPassword";
        var newPasswordHash = "hashedNewPassword";
        var oldSalt = "oldSalt";
        var newSalt = "newSalt";

        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            PhoneNumber = "+7777777777",
            Password = oldPasswordHash,
            Salt = oldSalt
        };

        var request = new UpdatePasswordRequest (
            oldPassword, 
            newPassword,
            confirmPassword);

        _userRepositoryMock.Setup(r => r.GetById(userId))
            .ReturnsAsync(user);

        _passwordServiceMock.Setup(p => p.ValidatePassword(oldPassword, oldSalt, oldPasswordHash))
            .Returns(true);

        _passwordServiceMock.Setup(p => p.CreatePasswordHash(newPassword))
            .Returns((newPasswordHash, newSalt));

        _userRepositoryMock.Setup(r => r.UpdatePassword(userId, It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _service.UpdatePassword(userId, request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Equal("Произошла ошибка при обновлении пароля", result.Errors[0].Message);
    }
} 