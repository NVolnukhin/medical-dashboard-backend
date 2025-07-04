using Microsoft.EntityFrameworkCore;

namespace AuthService.Repository.User
{
    public interface IUserRepository
    {
        public DbSet<AuthService.Models.User> Users { get; }

        Task SaveChangesAsync();
        
        public Task<AuthService.Models.User?> GetById(Guid userId);
        public  Task<AuthService.Models.User?> GetByEmail(string email);
        public Task UpdatePassword(Guid userId, string newPasswordHash, string newSalt);
    }
}
