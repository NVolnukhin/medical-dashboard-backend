using AuthService.DTOs.PasswordRecovery;
using FluentResults;

namespace AuthService.Services.PasswordRecovery;

public interface IPasswordRecoveryService
{
    Task<Result<PasswordRecoveryResponse>> RequestRecoveryAsync(string email);
    Task<Result<PasswordRecoveryResponse>> ConfirmRecoveryAsync(PasswordRecoveryConfirm request);
}