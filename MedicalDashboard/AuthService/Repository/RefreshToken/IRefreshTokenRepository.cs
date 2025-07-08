namespace AuthService.Repository.RefreshToken
{
    public interface IRefreshTokenRepository
    {
        Task<AuthService.Models.RefreshToken> CreateAsync(Models.RefreshToken token);
        Task<Models.RefreshToken> GetByTokenAsync(string token);
        Task RevokeTokenAsync(string token, string reason, string ipAddress);
        Task RevokeAllUserTokensAsync(Guid userId, string reason, string? ipAddress);
        Task<bool> IsTokenValidAsync(string token);
    }
} 