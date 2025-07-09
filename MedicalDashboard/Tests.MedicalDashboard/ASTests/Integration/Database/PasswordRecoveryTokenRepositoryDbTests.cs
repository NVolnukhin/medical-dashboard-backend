using AuthService.Models;
using AuthService.Repository.PasswordRecovery;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.MedicalDashboard.ASTests.Integration.Database;

public class PasswordRecoveryTokenRepositoryDbTests
{
    private AuthorizationAppContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthorizationAppContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AuthorizationAppContext(options);
    }

    [Fact]
    public async Task CanCreatePasswordRecoveryToken()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new PasswordRecoveryTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            Email = "test@example.com",
            Password = "hashed",
            Salt = "salt",
            PhoneNumber = "+777777777",
            IsActive = true,
            Role = "test"
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        var token = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            Token = "testToken",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            User = user
        };

        // Act
        await repository.CreateTokenAsync(token);

        // Assert
        var savedToken = await context.PasswordRecoveryTokens
            .FirstOrDefaultAsync(t => t.Token == "testToken");
        Assert.NotNull(savedToken);
        Assert.Equal(token.Token, savedToken!.Token);
        Assert.Equal(user.Id, savedToken.UserId);
        Assert.False(savedToken.IsUsed);
    }

    [Fact]
    public async Task GetValidTokenAsync_ReturnsToken_WhenTokenIsValid()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new PasswordRecoveryTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            Email = "test@example.com",
            Password = "hashed",
            Salt = "salt",
            PhoneNumber = "+777777777",
            IsActive = true,
            Role = "test"
        };

        var token = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            Token = "testToken",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            User = user
        };

        await context.Users.AddAsync(user);
        await context.PasswordRecoveryTokens.AddAsync(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetValidTokenAsync("testToken");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.Token, result!.Token);
        Assert.Equal(user.Id, result.UserId);
        Assert.False(result.IsUsed);
        Assert.NotNull(result.User);
        Assert.Equal(user.Email, result.User.Email);
    }

    [Fact]
    public async Task GetValidTokenAsync_ReturnsNull_WhenTokenIsUsed()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new PasswordRecoveryTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            Email = "test@example.com",
            Password = "hashed",
            Salt = "salt",
            PhoneNumber = "+777777777",
            IsActive = true,
            Role = "test"
        };

        var token = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            Token = "testToken",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = true,
            User = user
        };

        await context.Users.AddAsync(user);
        await context.PasswordRecoveryTokens.AddAsync(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetValidTokenAsync("testToken");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetValidTokenAsync_ReturnsNull_WhenTokenIsExpired()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new PasswordRecoveryTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            Email = "test@example.com",
            Password = "hashed",
            Salt = "salt",
            PhoneNumber = "+777777777",
            IsActive = true,
            Role = "test"
        };

        var token = new PasswordRecoveryToken
        {
            Id = Guid.NewGuid(),
            Token = "testToken",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired token
            IsUsed = false,
            User = user
        };

        await context.Users.AddAsync(user);
        await context.PasswordRecoveryTokens.AddAsync(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetValidTokenAsync("testToken");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidateUserTokensAsync_InvalidatesAllUserTokens()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new PasswordRecoveryTokenRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Андрей",
            LastName = "Петров",
            MiddleName = "Петрович",
            Email = "test@example.com",
            Password = "hashed",
            Salt = "salt",
            PhoneNumber = "+777777777",
            IsActive = true,
            Role = "test"
        };

        var tokens = new List<PasswordRecoveryToken>
        {
            new PasswordRecoveryToken
            {
                Id = Guid.NewGuid(),
                Token = "token1",
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false,
                User = user
            },
            new PasswordRecoveryToken
            {
                Id = Guid.NewGuid(),
                Token = "token2",
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsUsed = false,
                User = user
            }
        };

        await context.Users.AddAsync(user);
        await context.PasswordRecoveryTokens.AddRangeAsync(tokens);
        await context.SaveChangesAsync();

        // Act
        await repository.InvalidateUserTokensAsync(user.Id);

        // Assert
        var remainingTokens = await context.PasswordRecoveryTokens
            .Where(t => t.UserId == user.Id && !t.IsUsed)
            .ToListAsync();

        Assert.Empty(remainingTokens);
    }
} 