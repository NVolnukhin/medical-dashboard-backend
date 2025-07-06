using AuthService.DTOs;
using AuthService.Kafka;
using AuthService.Models;
using AuthService.Repository.PasswordRecovery;
using AuthService.Repository.User;
using AuthService.Services.Password;
using FluentResults;
using Shared.Extensions.Logging;

namespace AuthService.Services.User;

public class UserService: IUserService
{
    private readonly IPasswordRecoveryTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;
    private readonly IPasswordService _passwordService;
    private readonly IKafkaProducerService _notificationService;

    public UserService(
        ILogger<UserService> logger, 
        IPasswordRecoveryTokenRepository tokenRepository, 
        IUserRepository userRepository, 
        IPasswordService passwordService,
        IKafkaProducerService notificationService)
    {
        _logger = logger;
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _passwordService = passwordService;
        _notificationService = notificationService;
    }

    public async Task<Result> RecoverPassword(Guid userId, string newPassword, string confirmPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                return Result.Fail("Новый пароль не может быть пустым");
            }

            if (!newPassword.Equals(confirmPassword))
            {
                return Result.Fail("Пароли не совпадают");
            }

            var user = await _userRepository.GetById(userId);
            if (user == null)
            {
                _logger.LogWarning("Попытка восстановления пароля для несуществующего пользователя {UserId}", userId);
                return Result.Fail("Пользователь не найден");
            }   
        
            var (newPasswordHash, newSalt) = _passwordService.CreatePasswordHash(newPassword);
            
            await _userRepository.UpdatePassword(userId, newPasswordHash, newSalt);
            await _tokenRepository.InvalidateUserTokensAsync(userId);
    
            _logger.LogInfo($"Пароль успешно обновлён для пользователя {userId}");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при восстановлении пароля для пользователя {userId}", ex);
            return Result.Fail("Произошла ошибка при восстановлении пароля");
        }
    }
    
    public async Task<Result> UpdatePassword(Guid userId, UpdatePasswordRequest request)
    {
        try
        {
            var user = await _userRepository.GetById(userId);
            if (user == null)
            {
                _logger.LogWarning("Попытка изменения пароля для несуществующего юзера {UserId}", userId);
                return Result.Fail("Пользователь не найден");
            }

            if (!_passwordService.ValidatePassword(request.CurrentPassword, user.Salt, user.Password))
            {
                _logger.LogWarning("Неверный текущий пароль для {UserId}", userId);
                return Result.Fail("Неверный текущий пароль");
            }
            
            if (request.NewPassword.Length == 0)
            {
                _logger.LogWarning("Попытка установить пустой пароль для {UserId}", userId);
                return Result.Fail("Пароль не может быть пустым");
            }
            
            if (request.NewPassword.Length < 8)
            {
                _logger.LogWarning("Попытка установить слабый пароль для {UserId}", userId);
                return Result.Fail("Слишком короткий пароль");
            }
            
            if (!request.NewPassword.Equals(request.ConfirmPassword))
            {
                _logger.LogWarning("Пароли не совпадают при попытке изменения для {UserId}", userId);
                return Result.Fail("Пароли не совпадают");
            }
            
            var (newPasswordHash, newSalt) = _passwordService.CreatePasswordHash(request.NewPassword);
            await _userRepository.UpdatePassword(userId, newPasswordHash, newSalt);
            
            _logger.LogInfo($"Password updated for user {userId}");
            
            
            var message = new NotificationMessage
            {
                Type = 0,
                Recipient = user.Email,
                Subject = "Password changed",
                Body = "Password changed",
                Priority = 1,
                TemplateName = "Password changed",
                TemplateParameters = new Dictionary<string, string>
                {
                    { "userName", $"{user.FirstName} {user.LastName}" }
                }
            };

            await _notificationService.SendNotificationAsync(message);
            
            
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Error updating password for user {userId}", ex);
            return Result.Fail("Произошла ошибка при обновлении пароля");
        }
    }
}