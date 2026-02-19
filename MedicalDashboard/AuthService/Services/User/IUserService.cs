using AuthService.DTOs;
using FluentResults;

namespace AuthService.Services.User;

public interface IUserService
{
     public Task<Result> RecoverPassword(Guid userId, string newPassword, string confirmPassword);
     public Task<Result> UpdatePassword(Guid userId, UpdatePasswordRequest request);
}