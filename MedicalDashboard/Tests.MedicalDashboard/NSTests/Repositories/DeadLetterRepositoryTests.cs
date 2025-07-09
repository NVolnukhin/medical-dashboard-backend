using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Data.Models;
using NotificationService.Repositories.DeadLetter;

namespace Tests.MedicalDashboard.NSTests.Repositories
{
    public class DeadLetterRepositoryTests
    {
        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task AddAsync_AddsMessage()
        {
            using var context = CreateContext();
            var repo = new DeadLetterRepository(context);
            var msg = new DeadLetterMessage { Id = Guid.NewGuid() };
            var result = await repo.AddAsync(msg);
            Assert.Equal(msg, result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsList()
        {
            using var context = CreateContext();
            var repo = new DeadLetterRepository(context);
            var msg = new DeadLetterMessage { Id = Guid.NewGuid() };
            await repo.AddAsync(msg);
            var result = await repo.GetAllAsync();
            Assert.Contains(result, m => m.Id == msg.Id);
        }
    }
} 