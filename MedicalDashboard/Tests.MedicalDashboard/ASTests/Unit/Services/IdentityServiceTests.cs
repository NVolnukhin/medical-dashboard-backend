using AuthService.Models;
using AuthService.Repository.User;
using AuthService.Services.Identity;
using AuthService.Services.Jwt;
using AuthService.Services.Password;
using AuthService.Services.RefreshToken;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;

namespace Tests.MedicalDashboard.ASTests.Unit.Services;

public class IdentityServiceTests
{
    private readonly Mock<IUserRepository> _mockRepo;
    private readonly IdentityService _identityService;

    public IdentityServiceTests()
    {
        _mockRepo = new Mock<IUserRepository>();
        var mockPasswordService = new Mock<IPasswordService>();
        var mockJwtBuilder = new Mock<IJwtBuilder>();
        var mockRefreshTokenService = new Mock<IRefreshTokenService>();
        
        _identityService = new IdentityService(
            _mockRepo.Object,
            mockPasswordService.Object,
            mockJwtBuilder.Object,
            mockRefreshTokenService.Object
        );
    }
    
    [Fact]
    public async Task GetUserAsync_ReturnsUser_WhenUserExists()
    {
        // Arrange
        var testUser = new User
        {
            FirstName = "test",
            LastName = "test",
            MiddleName = "test",
            Email = "test@example.com",
            Password = "SecurePassword123!",
            PhoneNumber = "+7777777777",
            Role = "test"
        };

        //  mock DbSet
        var mockUsers = new List<User> { testUser }.AsQueryable();
        var mockDbSet = MockDbSet(mockUsers);

        _mockRepo.Setup(repo => repo.Users).Returns(mockDbSet);

        // Act
        var result = await _identityService.GetUserAsync("test@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("SecurePassword123!", result.Password);
    }

    //    
    [Fact]
    public async Task GetUserAsync_ReturnsNull_WhenUserNotExists()
    {
        // Arrange
        var mockUsers = new List<User>().AsQueryable();
        var mockDbSet = MockDbSet(mockUsers);

        _mockRepo.Setup(repo => repo.Users).Returns(mockDbSet);

        // Act
        var result = await _identityService.GetUserAsync("nonexistent@example.com");

        // Assert
        Assert.Null(result);
    }
    
    private static DbSet<T> MockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
        return mockSet.Object;
    }

    [Fact]
    public async Task InsertUserAsync_CallsRepository_WhenUserIsValid()
    {
        // Arrange
        var testUser = new User
        {
            FirstName = "test",
            LastName = "test",
            MiddleName = "test",
            Email = "new@example.com", 
            Password = "SecurePassword123!",
            PhoneNumber = "+7777777777",
            Role = "Nurse"
        };

        _mockRepo.Setup(repo => repo.Users.AddAsync(testUser, It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<EntityEntry<User>>(Task.FromResult((EntityEntry<User>)null)));

        _mockRepo.Setup(repo => repo.SaveChangesAsync())
            .Returns(Task.FromResult(1));

        // Act
        await _identityService.InsertUserAsync(testUser);

        // Assert
        _mockRepo.Verify(repo => repo.Users.AddAsync(testUser, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }
        
    [Fact]
    public async Task LoginAsync_ThrowsException_WhenPasswordIsInvalid()
    {
        // Arrange
        var user = new User
        {
            FirstName = "Демьянов",
            LastName = "Андрей",
            MiddleName = "Викторович",
            Email = "new@example.com", 
            Password = "SecurePassword123!",
            PhoneNumber = "+7777777777"
        };

        var mockPasswordService = new Mock<IPasswordService>();
        var mockJwtBuilder = new Mock<IJwtBuilder>();
        var mockRefreshTokenService = new Mock<IRefreshTokenService>();

        var mockRepo = new Mock<IUserRepository>();
        mockRepo.Setup(r => r.GetByEmail(user.Email)).ReturnsAsync(user);
        mockPasswordService.Setup(p => p.ValidatePassword("wrong", user.Salt, user.Password))
            .Returns(false);

        var service = new IdentityService(mockRepo.Object, mockPasswordService.Object,
            mockJwtBuilder.Object, mockRefreshTokenService.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.LoginAsync(user.Email, "wrong", "127.0.0.1"));

        Assert.Equal("Invalid email or password", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_ThrowsException_WhenUserNotFound()
    {
        // Arrange
        _mockRepo.Setup(repo => repo.GetByEmail("nonexistent@example.com"))
            .ReturnsAsync((User)null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _identityService.LoginAsync("nonexistent@example.com", "password", "127.0.0.1"));
        Assert.Equal("Invalid email or password", ex.Message);
    }

    
    [Fact]
    public async Task LoginAsync_ReturnsTokens_WhenLoginIsSuccessful()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Иванович",
            Email = "test@example.com",
            Password = "hashedPassword",
            Salt = "salt",
            PhoneNumber = "+7777777777",
            Role = "role"
        };

        var mockPasswordService = new Mock<IPasswordService>();
        var mockJwtBuilder = new Mock<IJwtBuilder>();
        var mockRefreshTokenService = new Mock<IRefreshTokenService>();

        mockPasswordService.Setup(p => p.ValidatePassword("password", user.Salt, user.Password))
            .Returns(true);
        mockJwtBuilder.Setup(j => j.GetTokenAsync(user.Id, user.Role))
            .ReturnsAsync("accessToken");
        mockRefreshTokenService.Setup(r => r.GenerateRefreshTokenAsync(user, "127.0.0.1"))
            .ReturnsAsync(new RefreshToken { Token = "refreshToken", UserId = user.Id });

        var service = new IdentityService(
            _mockRepo.Object,
            mockPasswordService.Object,
            mockJwtBuilder.Object,
            mockRefreshTokenService.Object);

        _mockRepo.Setup(repo => repo.GetByEmail(user.Email)).ReturnsAsync(user);

        // Act
        var result = await service.LoginAsync(user.Email, "password", "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("accessToken", result.AccessToken);
        Assert.Equal("refreshToken", result.RefreshToken);
        Assert.Equal("SUCCESS", result.Status);
        Assert.Equal("role", result.Role);
    }

    [Fact]
    public async Task RefreshTokenAsync_ThrowsException_WhenTokenIsInvalid()
    {
        // Arrange
        var mockRefreshTokenService = new Mock<IRefreshTokenService>();
        mockRefreshTokenService.Setup(r => r.ValidateRefreshTokenAsync("invalidToken"))
            .ReturnsAsync(false);

        var service = new IdentityService(
            _mockRepo.Object,
            new Mock<IPasswordService>().Object,
            new Mock<IJwtBuilder>().Object,
            mockRefreshTokenService.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RefreshTokenAsync("invalidToken", "127.0.0.1"));
        Assert.Equal("Invalid refresh token", ex.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_ReturnsNewTokens_WhenTokenIsValid()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Иван",
            LastName = "Иванов",
            MiddleName = "Иванович",
            Email = "test@example.com",
            Password = "hashedPassword",
            Salt = "salt",
            PhoneNumber = "+7777777777",
            Role = "test"
        };

        var mockRefreshTokenService = new Mock<IRefreshTokenService>();
        var mockJwtBuilder = new Mock<IJwtBuilder>();

        mockRefreshTokenService.Setup(r => r.ValidateRefreshTokenAsync("validToken"))
            .ReturnsAsync(true);
        mockRefreshTokenService.Setup(r => r.RotateRefreshTokenAsync("validToken", "127.0.0.1"))
            .ReturnsAsync(new RefreshToken { Token = "newRefreshToken", UserId = user.Id });
        mockJwtBuilder.Setup(j => j.GetTokenAsync(user.Id, user.Role))
            .ReturnsAsync("newAccessToken");

        _mockRepo.Setup(repo => repo.GetById(user.Id)).ReturnsAsync(user);

        var service = new IdentityService(
            _mockRepo.Object,
            new Mock<IPasswordService>().Object,
            mockJwtBuilder.Object,
            mockRefreshTokenService.Object);

        // Act
        var result = await service.RefreshTokenAsync("validToken", "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("newAccessToken", result.AccessToken);
        Assert.Equal("newRefreshToken", result.RefreshToken);
    }

    [Fact]
    public async Task RevokeTokenAsync_CallsRefreshTokenService()
    {
        // Arrange
        var mockRefreshTokenService = new Mock<IRefreshTokenService>();
        mockRefreshTokenService.Setup(r => r.RevokeRefreshTokenAsync("token", "127.0.0.1"))
            .Returns(Task.CompletedTask);

        var service = new IdentityService(
            _mockRepo.Object,
            new Mock<IPasswordService>().Object,
            new Mock<IJwtBuilder>().Object,
            mockRefreshTokenService.Object);

        // Act
        await service.RevokeTokenAsync("token", "127.0.0.1");

        // Assert
        mockRefreshTokenService.Verify(r => r.RevokeRefreshTokenAsync("token", "127.0.0.1"), Times.Once);
    }

    [Fact]
    public async Task ValidateTokenAsync_DelegatesToRefreshTokenService()
    {
        // Arrange
        var mockRefreshTokenService = new Mock<IRefreshTokenService>();
        mockRefreshTokenService.Setup(r => r.ValidateRefreshTokenAsync("token"))
            .ReturnsAsync(true);

        var service = new IdentityService(
            _mockRepo.Object,
            new Mock<IPasswordService>().Object,
            new Mock<IJwtBuilder>().Object,
            mockRefreshTokenService.Object);

        // Act
        var result = await service.ValidateTokenAsync("token");

        // Assert
        Assert.True(result);
        mockRefreshTokenService.Verify(r => r.ValidateRefreshTokenAsync("token"), Times.Once);
    }
}