using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repository.RefreshToken
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AuthorizationAppContext _context;

        public RefreshTokenRepository(AuthorizationAppContext context)
        {
            _context = context;
        }

        public async Task<Models.RefreshToken> CreateAsync(Models.RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }

        public async Task<Models.RefreshToken> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(r => r.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Token == token);
        }

        public async Task RevokeTokenAsync(string token, string reason, string ipAddress)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == token);
                
            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = ipAddress;
                refreshToken.ReasonRevoked = reason;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeAllUserTokensAsync(Guid userId, string reason, string? ipAddress)
        {
            var tokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && !r.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = ipAddress;
                token.ReasonRevoked = reason;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsTokenValidAsync(string token)
        {
            var refreshToken = await GetByTokenAsync(token);
            return refreshToken != null && 
                   !refreshToken.IsRevoked && 
                   refreshToken.ExpiresAt > DateTime.UtcNow;
        }
    }
} 