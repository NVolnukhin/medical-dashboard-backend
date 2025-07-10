using AuthService.Models;
using AuthService.Repository.RefreshToken;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.MedicalDashboard.ASTests.Unit.Repositories;

public class RefreshTokenRepositoryTests
{
    private readonly DbContextOptions<AuthorizationAppContext> _options;

    public RefreshTokenRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<AuthorizationAppContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task CreateAsync_WithValidToken_ShouldSaveToDatabase()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "test-salt"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "test-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        // Act
        var result = await repository.CreateAsync(token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-token", result.Token);
        Assert.Equal(user.Id, result.UserId);
        Assert.False(result.IsRevoked);

        var savedToken = await context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == "test-token");
        Assert.NotNull(savedToken);
    }

    [Fact]
    public async Task CreateAsync_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            repository.CreateAsync(null!));
    }

    [Fact]
    public async Task GetByTokenAsync_WithValidToken_ShouldReturnToken()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "test-salt"
        };

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "valid-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        context.Users.Add(user);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByTokenAsync("valid-token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("valid-token", result.Token);
        Assert.Equal(user.Id, result.UserId);
    }

    [Fact]
    public async Task GetByTokenAsync_WithNonExistentToken_ShouldReturnNull()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        // Act
        var result = await repository.GetByTokenAsync("non-existent-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTokenAsync_WithNullToken_ShouldReturnNull()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        // Act
        var result = await repository.GetByTokenAsync(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByTokenAsync_WithEmptyToken_ShouldReturnNull()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        // Act
        var result = await repository.GetByTokenAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RevokeTokenAsync_WithValidToken_ShouldMarkTokenAsRevoked()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "test-salt"
        };

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token-to-revoke",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        context.Users.Add(user);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        // Act
        await repository.RevokeTokenAsync("token-to-revoke", "Test reason", "127.0.0.1");

        // Assert
        var revokedToken = await context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == "token-to-revoke");
        Assert.NotNull(revokedToken);
        Assert.True(revokedToken.IsRevoked);
        Assert.NotNull(revokedToken.RevokedAt);
        Assert.Equal("Test reason", revokedToken.ReasonRevoked);
        Assert.Equal("127.0.0.1", revokedToken.RevokedByIp);
    }

    [Fact]
    public async Task RevokeTokenAsync_WithNonExistentToken_ShouldNotThrowException()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        // Act & Assert
        await repository.RevokeTokenAsync("non-existent-token", "Test reason", "127.0.0.1");
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_WithValidUserId_ShouldRevokeAllTokens()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "test-salt"
        };

        var token1 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token1",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        var token2 = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token2",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        context.Users.Add(user);
        context.RefreshTokens.AddRange(token1, token2);
        await context.SaveChangesAsync();

        // Act
        await repository.RevokeAllUserTokensAsync(user.Id, "Test reason", "127.0.0.1");

        // Assert
        var revokedTokens = await context.RefreshTokens
            .Where(t => t.UserId == user.Id)
            .ToListAsync();

        Assert.All(revokedTokens, token => Assert.True(token.IsRevoked));
        Assert.All(revokedTokens, token => Assert.NotNull(token.RevokedAt));
        Assert.All(revokedTokens, token => Assert.Equal("Test reason", token.ReasonRevoked));
        Assert.All(revokedTokens, token => Assert.Equal("127.0.0.1", token.RevokedByIp));
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_WithNonExistentUserId_ShouldNotThrowException()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        // Act & Assert
        await repository.RevokeAllUserTokensAsync(Guid.NewGuid(), "Test reason", "127.0.0.1");
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_WithEmptyUserId_ShouldNotThrowException()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        // Act & Assert
        await repository.RevokeAllUserTokensAsync(Guid.Empty, "Test reason", "127.0.0.1");
    }

    [Fact]
    public async Task IsTokenValidAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "test-salt"
        };

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "valid-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        context.Users.Add(user);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.IsTokenValidAsync("valid-token");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "test-salt"
        };

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "expired-token",
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        context.Users.Add(user);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.IsTokenValidAsync("expired-token");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_WithRevokedToken_ShouldReturnFalse()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "test-salt"
        };

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "revoked-token",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = true
        };

        context.Users.Add(user);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.IsTokenValidAsync("revoked-token");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_WithNonExistentToken_ShouldReturnFalse()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        // Act
        var result = await repository.IsTokenValidAsync("non-existent-token");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_WithNullToken_ShouldReturnFalse()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        // Act
        var result = await repository.IsTokenValidAsync(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_WithEmptyToken_ShouldReturnFalse()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new RefreshTokenRepository(context);

        // Act
        var result = await repository.IsTokenValidAsync("");

        // Assert
        Assert.False(result);
    }
} 