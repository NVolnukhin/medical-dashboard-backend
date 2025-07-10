using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repository.User
{
    public class UserRepository : IUserRepository
    {
        private readonly AuthorizationAppContext _authorizationAppContext;

        public UserRepository(AuthorizationAppContext authorizationAppContext)
        {
            _authorizationAppContext = authorizationAppContext;
        }

        public DbSet<AuthService.Models.User> Users => _authorizationAppContext.Users;
        public async Task SaveChangesAsync()
        {
            await _authorizationAppContext.SaveChangesAsync();
        }
        
        public async Task<AuthService.Models.User?> GetById(Guid userId)
        {
            return await _authorizationAppContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId) ?? throw new Exception("User not found");
        }

        public async Task<AuthService.Models.User?> GetByEmail(string email)
        {
            return await _authorizationAppContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task UpdatePassword(Guid userId, string newPasswordHash, string newSalt)
        {
            var user = await _authorizationAppContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.Password = newPasswordHash;
                user.Salt = newSalt;
                await _authorizationAppContext.SaveChangesAsync();
            }
        }
    }
}
