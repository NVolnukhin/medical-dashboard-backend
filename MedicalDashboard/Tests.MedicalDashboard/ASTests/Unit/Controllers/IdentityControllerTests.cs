using System.Net;
using AuthService.Controllers;
using AuthService.DTOs;
using AuthService.Kafka;
using AuthService.Models;
using AuthService.Services.Identity;
using AuthService.Services.Jwt;
using AuthService.Services.Password;
using AuthService.Services.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.MedicalDashboard.ASTests.Unit.Controllers;
public class IdentityControllerTests
{
    private readonly Mock<ILogger<PasswordRecoveryController>> _loggerMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly Mock<IJwtBuilder> _jwtBuilderMock = new();
    private readonly Mock<IPasswordService> _passwordServiceMock = new();
    private readonly Mock<IOneTimePasswordGenerator> _oneTimePasswordGeneratorMock = new ();
    private readonly Mock<IUserService> _userServiceMock = new();
    private readonly Mock<IKafkaProducerService> _kafkaProducerServiceMock = new();
    private readonly IdentityController _controller;

    public IdentityControllerTests()
    {
        _controller = new IdentityController(
            _identityServiceMock.Object,
            _jwtBuilderMock.Object,
            _passwordServiceMock.Object,
            _loggerMock.Object,
            _kafkaProducerServiceMock.Object,
            _oneTimePasswordGeneratorMock.Object,
            _userServiceMock.Object
        );
    }

    [Fact]
    public async Task Login_ReturnsOk_WhenCredentialsAreValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            PhoneNumber = "+7777777777",
            Id = userId,
            Email = "test@example.com",
            Password = "hashedPassword",
            Salt = "salt"
        };

        var loginResponse = new LoginResponse
        {
            AccessToken = "mockToken",
            RefreshToken = "refreshToken",
            Status = "SUCCESS",
            Role = "role"
        };

        _identityServiceMock.Setup(s => s.GetUserAsync(user.Email))
            .ReturnsAsync(user);
    
        _passwordServiceMock.Setup(s => s.ValidatePassword("password", user.Salt, user.Password))
            .Returns(true);

        _identityServiceMock.Setup(s => s.LoginAsync(user.Email, "password", "127.0.0.1"))
            .ReturnsAsync(loginResponse);

        var request = new LoginRequest
        {
            Email = user.Email,
            Password = "password"
        };
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LoginResponse>(okResult.Value);
        Assert.Equal("mockToken", response.AccessToken);
        Assert.Equal("refreshToken", response.RefreshToken);
        Assert.Equal("SUCCESS", response.Status);
    }

    [Fact]
    public async Task Login_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        _identityServiceMock.Setup(s => s.GetUserAsync("test@example.com"))
                            .ReturnsAsync((User)null);

        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found.", notFound.Value);
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenPasswordIsInvalid()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            Email = "test@example.com",
            Password = "hashed",
            Salt = "salt",
            PhoneNumber = "+7777777777"
        };

        _identityServiceMock.Setup(s => s.GetUserAsync(user.Email))
            .ReturnsAsync(user);

        _identityServiceMock.Setup(s => s.LoginAsync(user.Email, "wrong", It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Invalid email or password"));

        var request = new LoginRequest
        {
            Email = user.Email,
            Password = "wrong"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value); // просто убедимся, что значение есть
        var message = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value) as string;
        Assert.Equal("Could not authenticate user.", message);
    }

    [Fact]
    public async Task Register_ReturnsOk_WhenUserIsCreated()
    {
        var request = new RegisterRequest
        {
            Email = "new@example.com"
        };
        _oneTimePasswordGeneratorMock.Setup(s => s.GeneratePassword(8))
            .Returns("password");

        _identityServiceMock.Setup(s => s.GetUserAsync(request.Email))
                            .ReturnsAsync((User)null);

        _passwordServiceMock.Setup(p => p.CreatePasswordHash("rw4234nb13dsa31"))
                            .Returns(("hash", "salt"));

        _identityServiceMock.Setup(s => s.InsertUserAsync(It.IsAny<User>()))
                            .Returns(Task.CompletedTask);

        var result = await _controller.Register(request);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenUserExists()
    {
        var request = new RegisterRequest
        {
            Email = "existing@example.com"
        };

        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            Email = request.Email,
            Password = "hashedPassword",
            IsActive = true,
            PhoneNumber = "+7777777777"
        };

        _identityServiceMock.Setup(s => s.GetUserAsync(request.Email))
                            .ReturnsAsync(existingUser);

        var result = await _controller.Register(request);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("User already exists.", bad.Value);
    }


    [Fact]
    public async Task Validate_ReturnsOk_WhenTokenIsValid()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId,
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            Password = "password",
            Email = "test@example.com",
            PhoneNumber = "+7777777777"
        };

        _identityServiceMock.Setup(s => s.GetUserAsync(user.Email))
                            .ReturnsAsync(user);

        _jwtBuilderMock.Setup(j => j.ValidateToken("validToken"))
                       .Returns(userId.ToString());

        var result = await _controller.Validate(user.Email, "validToken");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(userId.ToString(), ok.Value);
    }

    [Fact]
    public async Task Validate_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _identityServiceMock.Setup(s => s.GetUserAsync("missing@example.com")) 
                            .ReturnsAsync((User)null);

        var result = await _controller.Validate("missing@example.com", "token");

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("User not found.", notFound.Value);
    }

    [Fact]
    public async Task Validate_ReturnsBadRequest_WhenTokenIsInvalid()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId,
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            Password = "password", 
            Email = "test@example.com",
            PhoneNumber = "+7777777777"
        };

        _identityServiceMock.Setup(s => s.GetUserAsync(user.Email))
                            .ReturnsAsync(user);

        _jwtBuilderMock.Setup(j => j.ValidateToken("invalid"))
                       .Returns(Guid.NewGuid().ToString()); // другой  ID 

        var result = await _controller.Validate(user.Email, "invalid");

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid token.", bad.Value);
    }

    [Fact]
    public async Task RefreshToken_ReturnsOk_WhenTokensAreValid()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "oldRefreshToken"
        };

        var tokensResponse = new TokensResponse
        {
            AccessToken = "newAccessToken",
            RefreshToken = "newRefreshToken"
        };

        _identityServiceMock.Setup(s => s.RefreshTokenAsync(request.RefreshToken, "127.0.0.1"))
            .ReturnsAsync(tokensResponse);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TokensResponse>(okResult.Value);
        Assert.Equal("newAccessToken", response.AccessToken);
        Assert.Equal("newRefreshToken", response.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_ReturnsBadRequest_WhenTokenIsInvalid()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalidToken"
        };

        _identityServiceMock.Setup(s => s.RefreshTokenAsync(request.RefreshToken, It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Invalid refresh token"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value); // просто убедимся, что значение есть
        var message = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value) as string;
        Assert.Equal("Invalid refresh token", message);
    }

    [Fact]
    public async Task RevokeToken_ReturnsOk_WhenTokenIsValid()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "validToken"
        };

        _identityServiceMock.Setup(s => s.RevokeTokenAsync(request.RefreshToken, "127.0.0.1"))
            .Returns(Task.CompletedTask);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        // Act
        var result = await _controller.RevokeToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value); // просто убедимся, что значение есть
        var message = okResult.Value.GetType().GetProperty("message")?.GetValue(okResult.Value) as string;
        Assert.Equal("Токен успешно отозван", message);
    }

    [Fact]
    public async Task RevokeToken_ReturnsBadRequest_WhenTokenIsInvalid()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "invalidToken"
        };

        _identityServiceMock.Setup(s => s.RevokeTokenAsync(request.RefreshToken, It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Invalid refresh token"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        _controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        // Act
        var result = await _controller.RevokeToken(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value); // просто убедимся, что значение есть
        var message = badRequestResult.Value.GetType().GetProperty("message")?.GetValue(badRequestResult.Value) as string;
        Assert.Equal("Invalid refresh token", message);
    }
}
