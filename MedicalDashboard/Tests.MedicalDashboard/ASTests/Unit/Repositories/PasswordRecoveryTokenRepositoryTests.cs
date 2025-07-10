using AuthService.Models;
using AuthService.Repository.PasswordRecovery;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.MedicalDashboard.ASTests.Unit.Repositories;

public class PasswordRecoveryTokenRepositoryTests
{
    private readonly DbContextOptions<AuthorizationAppContext> _options;

    public PasswordRecoveryTokenRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<AuthorizationAppContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetValidTokenAsync_WithValidToken_ShouldReturnToken()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new PasswordRecoveryTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "salt123"
        };

        var token = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "valid-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        context.Users.Add(user);
        context.PasswordRecoveryTokens.Add(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetValidTokenAsync("valid-token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("valid-token", result.Token);
        Assert.Equal(user.Id, result.UserId);
        Assert.False(result.IsUsed);
    }

    [Fact]
    public async Task GetValidTokenAsync_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new PasswordRecoveryTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "salt123"
        };

        var token = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "expired-token",
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            IsUsed = false
        };

        context.Users.Add(user);
        context.PasswordRecoveryTokens.Add(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetValidTokenAsync("expired-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidTokenAsync_WithUsedToken_ShouldReturnNull()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new PasswordRecoveryTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "salt123"
        };

        var token = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "used-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = true
        };

        context.Users.Add(user);
        context.PasswordRecoveryTokens.Add(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetValidTokenAsync("used-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidTokenAsync_WithNonExistentToken_ShouldReturnNull()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new PasswordRecoveryTokenRepository(context);

        // Act
        var result = await repository.GetValidTokenAsync("non-existent-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidTokenAsync_WithNullToken_ShouldReturnNull()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new PasswordRecoveryTokenRepository(context);

        // Act
        var result = await repository.GetValidTokenAsync(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidTokenAsync_WithEmptyToken_ShouldReturnNull()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new PasswordRecoveryTokenRepository(context);

        // Act
        var result = await repository.GetValidTokenAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateTokenAsync_WithValidToken_ShouldSaveToDatabase()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new PasswordRecoveryTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "salt123"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "new-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        // Act
        await repository.CreateTokenAsync(token);

        // Assert
        var savedToken = await context.PasswordRecoveryTokens.FirstOrDefaultAsync(t => t.Token == "new-token");
        Assert.NotNull(savedToken);
        Assert.Equal(user.Id, savedToken.UserId);
        Assert.False(savedToken.IsUsed);
    }

    [Fact]
    public async Task CreateTokenAsync_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new PasswordRecoveryTokenRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            repository.CreateTokenAsync(null!));
    }

    [Fact]
    public async Task CreateTokenAsync_WithDuplicateToken_ShouldThrowException()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new PasswordRecoveryTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "salt123"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var existingToken = new PasswordRecoveryToken
        {
            Id = Guid.Parse("db0768e7-ef2e-4515-883b-b4d27bae0e00"),
            UserId = user.Id,
            Token = "duplicate-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        context.PasswordRecoveryTokens.Add(existingToken);
        await context.SaveChangesAsync();

        var duplicateToken = new PasswordRecoveryToken
        {
            Id = Guid.Parse("db0768e7-ef2e-4515-883b-b4d27bae0e00"),
            UserId = user.Id,
            Token = "duplicate-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            repository.CreateTokenAsync(duplicateToken));
    }

    [Fact]
    public async Task InvalidateUserTokensAsync_WithValidUserId_ShouldMarkTokensAsUsed()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new PasswordRecoveryTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "salt123"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token1 = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token1",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        var token2 = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "token2",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        context.PasswordRecoveryTokens.AddRange(token1, token2);
        await context.SaveChangesAsync();

        // Act
        await repository.InvalidateUserTokensAsync(user.Id);

        // Assert
        var updatedTokens = await context.PasswordRecoveryTokens
            .Where(t => t.UserId == user.Id)
            .ToListAsync();

        Assert.All(updatedTokens, token => Assert.True(token.IsUsed));
    }

    [Fact]
    public async Task InvalidateUserTokensAsync_WithNonExistentUserId_ShouldNotThrowException()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new PasswordRecoveryTokenRepository(context);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => 
            repository.InvalidateUserTokensAsync(Guid.NewGuid()));
        Assert.Null(exception);
    }

    [Fact]
    public async Task InvalidateUserTokensAsync_WithEmptyUserId_ShouldNotThrowException()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new PasswordRecoveryTokenRepository(context);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => 
            repository.InvalidateUserTokensAsync(Guid.Empty));
        Assert.Null(exception);
    }

    [Fact]
    public async Task InvalidateUserTokensAsync_WithAlreadyUsedTokens_ShouldNotChangeTokens()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new PasswordRecoveryTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Password = "password123",
            PhoneNumber = "+1234567890",
            Role = "User",
            Salt = "salt123"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var usedToken = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "used-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = true
        };

        context.PasswordRecoveryTokens.Add(usedToken);
        await context.SaveChangesAsync();

        // Act
        await repository.InvalidateUserTokensAsync(user.Id);

        // Assert
        var token = await context.PasswordRecoveryTokens.FirstOrDefaultAsync(t => t.Id == usedToken.Id);
        Assert.NotNull(token);
        Assert.True(token.IsUsed);
    }
} 