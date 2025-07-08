using AuthService.DTOs;

namespace AuthService.Services.Identity
{
    public interface IIdentityService
    {
        Task<AuthService.Models.User?> GetUserAsync(string email);
        Task<AuthService.Models.User?> GetUserByIdAsync(Guid userId);
        Task InsertUserAsync(AuthService.Models.User user);
        Task<LoginResponse> LoginAsync(string email, string password, string? ipAddress);
        Task<TokensResponse> RefreshTokenAsync(string refreshToken, string? ipAddress);
        Task RevokeTokenAsync(string refreshToken, string? ipAddress);
    }
}
