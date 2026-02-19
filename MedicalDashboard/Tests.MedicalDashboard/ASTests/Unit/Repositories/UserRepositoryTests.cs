using AuthService.Models;
using AuthService.Repository.User;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.MedicalDashboard.ASTests.Unit.Repositories;

public class UserRepositoryTests
{
    private readonly DbContextOptions<AuthorizationAppContext> _options;

    public UserRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<AuthorizationAppContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task GetById_WithValidId_ShouldReturnUser()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new UserRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PhoneNumber = "+1234567890",
            Password = "password123",
            Salt = "salt123",
            Role = "User"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetById(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ShouldThrowException()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new UserRepository(context);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => repository.GetById(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByEmail_WithValidEmail_ShouldReturnUser()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new UserRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PhoneNumber = "+1234567890",
            Password = "password123",
            Salt = "salt123",
            Role = "User"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.GetByEmail("test@example.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Email, result.Email);
    }

    [Fact]
    public async Task GetByEmail_WithInvalidEmail_ShouldReturnNull()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByEmail("invalid@example.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmail_WithNullEmail_ShouldReturnNull()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByEmail(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmail_WithEmptyEmail_ShouldReturnNull()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new UserRepository(context);

        // Act
        var result = await repository.GetByEmail("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdatePassword_WithValidUser_ShouldUpdatePassword()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new UserRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PhoneNumber = "+1234567890",
            Password = "oldpassword",
            Salt = "oldsalt",
            Role = "User"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var newPasswordHash = "newpasswordhash";
        var newSalt = "newsalt";

        // Act
        await repository.UpdatePassword(user.Id, newPasswordHash, newSalt);

        // Assert
        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(newPasswordHash, updatedUser.Password);
        Assert.Equal(newSalt, updatedUser.Salt);
    }

    [Fact]
    public async Task UpdatePassword_WithNonExistentUser_ShouldNotThrowException()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new UserRepository(context);

        // Act & Assert
        await repository.UpdatePassword(Guid.NewGuid(), "newpassword", "newsalt");
    }

    [Fact]
    public async Task UpdatePassword_WithEmptyPassword_ShouldUpdatePassword()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new UserRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PhoneNumber = "+1234567890",
            Password = "oldpassword",
            Salt = "oldsalt",
            Role = "User"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        await repository.UpdatePassword(user.Id, "", "");

        // Assert
        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("", updatedUser.Password);
        Assert.Equal("", updatedUser.Salt);
    }

    [Fact]
    public async Task UpdatePassword_WithNullPassword_ShouldUpdatePassword()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new UserRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PhoneNumber = "+1234567890",
            Password = "oldpassword",
            Salt = "oldsalt",
            Role = "User"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        await repository.UpdatePassword(user.Id, null!, null!);

        // Assert
        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(updatedUser);
        Assert.Null(updatedUser.Password);
        Assert.Null(updatedUser.Salt);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSaveChanges()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new UserRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PhoneNumber = "+1234567890",
            Password = "password123",
            Salt = "salt123",
            Role = "User"
        };

        context.Users.Add(user);

        // Act
        await repository.SaveChangesAsync();

        // Assert
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(savedUser);
    }

    [Fact]
    public async Task Users_ShouldReturnDbSet()
    {
        // Arrange
        using var context = new AuthorizationAppContext(_options);
        var repository = new UserRepository(context);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PhoneNumber = "+1234567890",
            Password = "password123",
            Salt = "salt123",
            Role = "User"
        };

        repository.Users.Add(user);
        await repository.SaveChangesAsync();

        // Act
        var users = repository.Users.ToList();

        // Assert
        Assert.Single(users);
        Assert.Equal(user.Id, users[0].Id);
    }
} 