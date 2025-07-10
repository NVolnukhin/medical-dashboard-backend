using Microsoft.EntityFrameworkCore;

namespace AuthService.Repository.User
{
    public interface IUserRepository
    {
        public DbSet<AuthService.Models.User> Users { get; }

        Task SaveChangesAsync();
        
        public Task<AuthService.Models.User?> GetById(Guid userId);
        public Task<AuthService.Models.User?> GetByEmail(string email);
        public Task UpdatePassword(Guid userId, string newPasswordHash, string newSalt);
        public Task<IEnumerable<AuthService.Models.User>> GetAllAsync(int page = 1, int pageSize = 20, string? emailFilter = null, string? roleFilter = null);
        public Task<int> GetTotalCountAsync(string? emailFilter = null, string? roleFilter = null);
    }
}
