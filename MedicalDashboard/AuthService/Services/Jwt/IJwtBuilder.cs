using Shared;

namespace AuthService.Services.Jwt
{
    public interface IJwtBuilder
    {
        Task<string> GetTokenAsync(Guid userId, string role);
        string ValidateToken(string token);
    }
}
