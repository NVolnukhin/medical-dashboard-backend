using AuthService.Models;
using AuthService.Repository.User;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Tests.MedicalDashboard.ASTests.Integration.Database;
public class UserRepositoryDbTests
{
    private AuthorizationAppContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthorizationAppContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // каждый тест — изолированная БД
            .Options;

        return new AuthorizationAppContext(options);
    }

    [Fact]
    public async Task CanInsertUserIntoDatabase()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var repository = new UserRepository(context);

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
            Role = "test",
        };

        await repository.Users.AddAsync(user);

        // Act
        await repository.SaveChangesAsync();

        // Assert
        var savedUser = await repository.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotNull(savedUser);
        Assert.Equal(user.Email, savedUser!.Email);
    }
}
