namespace AuthService.Services.Jwt
{
    public interface IJwtBuilder
    {
        Task<string> GetTokenAsync(Guid userId);
        string ValidateToken(string token);
    }
}
