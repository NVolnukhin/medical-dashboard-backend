using AuthService.Models;

namespace AuthService.Repository.PasswordRecovery;

public interface IPasswordRecoveryTokenRepository
{
    Task<PasswordRecoveryToken?> GetValidTokenAsync(string token);
    Task CreateTokenAsync(PasswordRecoveryToken token);
    Task InvalidateUserTokensAsync(Guid userId);
}