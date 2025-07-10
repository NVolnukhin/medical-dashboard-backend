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

        public async Task<IEnumerable<AuthService.Models.User>> GetAllAsync(int page = 1, int pageSize = 20, string? emailFilter = null, string? roleFilter = null)
        {
            var query = _authorizationAppContext.Users.AsNoTracking();

            if (!string.IsNullOrEmpty(emailFilter))
            {
                query = query.Where(u => u.Email.Contains(emailFilter));
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                query = query.Where(u => u.Role == roleFilter);
            }

            return await query
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(string? emailFilter = null, string? roleFilter = null)
        {
            var query = _authorizationAppContext.Users.AsNoTracking();

            if (!string.IsNullOrEmpty(emailFilter))
            {
                query = query.Where(u => u.Email.Contains(emailFilter));
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                query = query.Where(u => u.Role == roleFilter);
            }

            return await query.CountAsync();
        }
    }
}
