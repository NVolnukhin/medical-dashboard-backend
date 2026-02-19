using AuthService.Models;
using AuthService.Repository.RefreshToken;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.MedicalDashboard.ASTests.Integration.Database;

public class RefreshTokenRepositoryDbTests
{
    private AuthorizationAppContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthorizationAppContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AuthorizationAppContext(options);
    }

    [Fact]
    public async Task CanCreateRefreshToken()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new RefreshTokenRepository(context);

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

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "testToken",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            User = user
        };

        // Act
        await repository.CreateAsync(token);

        // Assert
        var savedToken = await context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == "testToken");
        Assert.NotNull(savedToken);
        Assert.Equal(token.Token, savedToken!.Token);
        Assert.Equal(user.Id, savedToken.UserId);
        Assert.False(savedToken.IsRevoked);
    }

    [Fact]
    public async Task GetByTokenAsync_ReturnsToken_WhenTokenExists()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new RefreshTokenRepository(context);

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

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "testToken",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            User = user
        };

        await context.Users.AddAsync(user);
        await context.RefreshTokens.AddAsync(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByTokenAsync("testToken");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(token.Token, result!.Token);
        Assert.Equal(user.Id, result.UserId);
        Assert.False(result.IsRevoked);
        Assert.NotNull(result.User);
        Assert.Equal(user.Email, result.User.Email);
    }

    [Fact]
    public async Task GetByTokenAsync_ReturnsNull_WhenTokenDoesNotExist()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new RefreshTokenRepository(context);

        // Act
        var result = await repository.GetByTokenAsync("nonexistentToken");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RevokeTokenAsync_MarksTokenAsRevoked()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new RefreshTokenRepository(context);

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

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "testToken",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            User = user
        };

        await context.Users.AddAsync(user);
        await context.RefreshTokens.AddAsync(token);
        await context.SaveChangesAsync();

        // Act
        await repository.RevokeTokenAsync(token.Token, "logout", "127.0.0.1");

        // Assert
        var updatedToken = await context.RefreshTokens.FindAsync(token.Id);
        Assert.NotNull(updatedToken);
        Assert.True(updatedToken!.IsRevoked);
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_RevokesAllUserTokens()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new RefreshTokenRepository(context);

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

        var tokens = new List<RefreshToken>
        {
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "token1",
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                User = user
            },
            new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "token2",
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow,
                User = user
            }
        };

        await context.Users.AddAsync(user);
        await context.RefreshTokens.AddRangeAsync(tokens);
        await context.SaveChangesAsync();

        // Act
        await repository.RevokeAllUserTokensAsync(user.Id, "logout", "127.0.0.1");

        // Assert
        var remainingTokens = await context.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ToListAsync();

        Assert.Empty(remainingTokens);
    }

    [Fact]
    public async Task IsTokenValidAsync_ReturnsTrue_WhenTokenIsValid()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new RefreshTokenRepository(context);

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

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "testToken",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            User = user
        };

        await context.Users.AddAsync(user);
        await context.RefreshTokens.AddAsync(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.IsTokenValidAsync(token.Token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_ReturnsFalse_WhenTokenIsRevoked()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new RefreshTokenRepository(context);

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

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "testToken",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true,
            CreatedAt = DateTime.UtcNow,
            User = user
        };

        await context.Users.AddAsync(user);
        await context.RefreshTokens.AddAsync(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.IsTokenValidAsync(token.Token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsTokenValidAsync_ReturnsFalse_WhenTokenIsExpired()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new RefreshTokenRepository(context);

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

        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "testToken",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // Expired token
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            User = user
        };

        await context.Users.AddAsync(user);
        await context.RefreshTokens.AddAsync(token);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.IsTokenValidAsync(token.Token);

        // Assert
        Assert.False(result);
    }
} 