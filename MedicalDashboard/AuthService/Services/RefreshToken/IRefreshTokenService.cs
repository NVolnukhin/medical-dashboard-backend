namespace AuthService.Services.RefreshToken
{
    public interface IRefreshTokenService
    {
        Task<Models.RefreshToken> GenerateRefreshTokenAsync(Models.User user, string? ipAddress);
        Task<Models.RefreshToken> RotateRefreshTokenAsync(string token, string? ipAddress);
        Task RevokeRefreshTokenAsync(string token, string? ipAddress);
        Task<bool> ValidateRefreshTokenAsync(string token);
    }
} 