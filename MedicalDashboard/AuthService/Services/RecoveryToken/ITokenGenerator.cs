namespace AuthService.Services.RecoveryToken;

public interface ITokenGenerator
{
    string GenerateToken();
}