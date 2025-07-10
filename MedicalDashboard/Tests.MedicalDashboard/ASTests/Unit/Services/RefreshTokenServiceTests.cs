using AuthService.Services.RefreshToken;
using AuthService.Repository.RefreshToken;
using AuthService.Models;
using Moq;
using Xunit;

namespace Tests.MedicalDashboard.ASTests.Unit.Services;

public class RefreshTokenServiceTests
{
    private readonly Mock<IRefreshTokenRepository> _mockRepository;
    private readonly RefreshTokenService _refreshTokenService;

    public RefreshTokenServiceTests()
    {
        _mockRepository = new Mock<IRefreshTokenRepository>();
        _refreshTokenService = new RefreshTokenService(_mockRepository.Object);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_WithValidUser_ShouldReturnRefreshToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890"
        };

        var expectedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _mockRepository.Setup(x => x.RevokeAllUserTokensAsync(user.Id, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync(expectedToken);

        // Act
        var result = await _refreshTokenService.GenerateRefreshTokenAsync(user, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.UserId);
        Assert.False(result.IsRevoked);
        _mockRepository.Verify(x => x.RevokeAllUserTokensAsync(user.Id, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockRepository.Verify(x => x.CreateAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateRefreshTokenAsync_WithNullUser_ShouldThrowNullReferenceException()
    {
        // Arrange
        User? user = null;
        var ipAddress = "192.168.1.1";

        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => 
            _refreshTokenService.GenerateRefreshTokenAsync(user, ipAddress));
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_WithValidToken_ShouldReturnNewToken()
    {
        // Arrange
        var oldToken = "old-token";
        var newToken = "new-token";
        var userId = Guid.NewGuid();

        var existingRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = oldToken,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        var expectedNewToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = newToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false,
            ReplacedByToken = oldToken
        };

        _mockRepository.Setup(x => x.GetByTokenAsync(oldToken))
            .ReturnsAsync(existingRefreshToken);
        _mockRepository.Setup(x => x.RevokeTokenAsync(oldToken, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockRepository.Setup(x => x.CreateAsync(It.IsAny<RefreshToken>()))
            .ReturnsAsync(expectedNewToken);

        // Act
        var result = await _refreshTokenService.RotateRefreshTokenAsync(oldToken, "127.0.0.1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(oldToken, result.ReplacedByToken);
        _mockRepository.Verify(x => x.RevokeTokenAsync(oldToken, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        _mockRepository.Verify(x => x.CreateAsync(It.IsAny<RefreshToken>()), Times.Once);
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_WithInvalidToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidToken = "invalid-token";
        _mockRepository.Setup(x => x.GetByTokenAsync(invalidToken))
            .ReturnsAsync((RefreshToken)null!);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _refreshTokenService.RotateRefreshTokenAsync(invalidToken, "127.0.0.1"));
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_WithRevokedToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var revokedToken = "revoked-token";
        var existingRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = revokedToken,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = true
        };

        _mockRepository.Setup(x => x.GetByTokenAsync(revokedToken))
            .ReturnsAsync(existingRefreshToken);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _refreshTokenService.RotateRefreshTokenAsync(revokedToken, "127.0.0.1"));
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldRevokeToken()
    {
        // Arrange
        var token = "valid-token";
        var existingRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _mockRepository.Setup(x => x.GetByTokenAsync(token))
            .ReturnsAsync(existingRefreshToken);
        _mockRepository.Setup(x => x.RevokeTokenAsync(token, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _refreshTokenService.RevokeRefreshTokenAsync(token, "127.0.0.1");

        // Assert
        _mockRepository.Verify(x => x.RevokeTokenAsync(token, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithInvalidToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var invalidToken = "invalid-token";
        _mockRepository.Setup(x => x.GetByTokenAsync(invalidToken))
            .ReturnsAsync((RefreshToken)null!);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _refreshTokenService.RevokeRefreshTokenAsync(invalidToken, "127.0.0.1"));
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithRevokedToken_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var revokedToken = "revoked-token";
        var existingRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Token = revokedToken,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = true
        };

        _mockRepository.Setup(x => x.GetByTokenAsync(revokedToken))
            .ReturnsAsync(existingRefreshToken);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _refreshTokenService.RevokeRefreshTokenAsync(revokedToken, "127.0.0.1"));
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var token = "valid-token";
        _mockRepository.Setup(x => x.IsTokenValidAsync(token))
            .ReturnsAsync(true);

        // Act
        var result = await _refreshTokenService.ValidateRefreshTokenAsync(token);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(x => x.IsTokenValidAsync(token), Times.Once);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var token = "invalid-token";
        _mockRepository.Setup(x => x.IsTokenValidAsync(token))
            .ReturnsAsync(false);

        // Act
        var result = await _refreshTokenService.ValidateRefreshTokenAsync(token);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(x => x.IsTokenValidAsync(token), Times.Once);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithNullToken_ShouldReturnFalse()
    {
        // Arrange
        string? token = null;
        _mockRepository.Setup(x => x.IsTokenValidAsync(token!))
            .ReturnsAsync(false);

        // Act
        var result = await _refreshTokenService.ValidateRefreshTokenAsync(token!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithEmptyToken_ShouldReturnFalse()
    {
        // Arrange
        var token = "";
        _mockRepository.Setup(x => x.IsTokenValidAsync(token))
            .ReturnsAsync(false);

        // Act
        var result = await _refreshTokenService.ValidateRefreshTokenAsync(token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WithWhitespaceToken_ShouldReturnFalse()
    {
        // Arrange
        var token = "   ";
        _mockRepository.Setup(x => x.IsTokenValidAsync(token))
            .ReturnsAsync(false);

        // Act
        var result = await _refreshTokenService.ValidateRefreshTokenAsync(token);

        // Assert
        Assert.False(result);
    }
} 