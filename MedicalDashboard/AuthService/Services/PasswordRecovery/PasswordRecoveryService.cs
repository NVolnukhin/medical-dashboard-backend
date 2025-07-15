using AuthService.DTOs.PasswordRecovery;
using AuthService.Kafka;
using AuthService.Models;
using AuthService.Repository.PasswordRecovery;
using AuthService.Repository.User;
using AuthService.Services.RecoveryToken;
using AuthService.Services.User;
using FluentResults;
using Shared.Extensions.Logging;

namespace AuthService.Services.PasswordRecovery;

public class PasswordRecoveryService : IPasswordRecoveryService
{
    private readonly IPasswordRecoveryTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<PasswordRecoveryService> _logger;
    private readonly IUserService _userService;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IKafkaProducerService _notificationService;


    public PasswordRecoveryService(
        IPasswordRecoveryTokenRepository tokenRepository,
        IUserRepository userRepository,
        ILogger<PasswordRecoveryService> logger,
        IUserService userService,
        ITokenGenerator tokenGenerator,
        IKafkaProducerService notificationService)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _logger = logger;
        _userService = userService;
        _tokenGenerator = tokenGenerator;
        _notificationService = notificationService;
    }

    public async Task<Result<PasswordRecoveryResponse>> RequestRecoveryAsync(string email)
    {
        try
        {
            var user = await _userRepository.GetByEmail(email);
            if (user is null)
            {
                return Result.Ok(new PasswordRecoveryResponse(
                    true,
                    "Если аккаунт с таким email существует, инструкции будут отправлены"));
            }

            await _tokenRepository.InvalidateUserTokensAsync(user.Id);

            var token = new PasswordRecoveryToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = _tokenGenerator.GenerateToken(),
                ExpiresAt = DateTime.UtcNow.AddHours(12),
                IsUsed = false
            };

            await _tokenRepository.CreateTokenAsync(token);
            
            try
            {
                var message = new NotificationMessage
                {
                    Type = 0,
                    Recipient = email,
                    Subject = "Change password request",
                    Body = "Change password request",
                    Priority = 0,
                    TemplateName = "Change password request",
                    TemplateParameters = new Dictionary<string, string>
                    {
                        { "userName", $"{user.FirstName} {user.LastName}" },
                        { "recoveryLink", $"http://localhost:5173/recover-password?token={token.Token}" },
                        { "hoursValid", "12" }
                    }
                };
                
                await _notificationService.SendNotificationAsync(message);

                
                return Result.Ok(new PasswordRecoveryResponse(
                    true,
                    "Если аккаунт с таким email существует, письмо отправлено"));
            }
            catch (Exception ex)
            {
                _logger.LogFailure("Error sending recovery email", ex);
                return Result.Fail("Ошибка при отправке письма с инструкциями");
            }
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Error requesting password recovery", ex);
            return Result.Fail("Ошибка при запросе восстановления пароля");
        }
    }

    public async Task<Result<PasswordRecoveryResponse>> ConfirmRecoveryAsync(PasswordRecoveryConfirm request)
    {
        try
        {
            var token = await _tokenRepository.GetValidTokenAsync(request.Token);
            if (token is null)
            {
                return Result.Fail("Недействительная или просроченная ссылка");
            }

            var result = await _userService.RecoverPassword(
                token.UserId,
                request.NewPassword,
                request.ConfirmPassword);

            if (result.IsFailed)
            {
                return Result.Fail(result.Errors);
            }
            token.IsUsed = true;
            await _tokenRepository.CreateTokenAsync(token);

            _logger.LogInfo($"Password recovered for user {token.UserId}");
            
            var user = await _userRepository.GetById(token.UserId);
            
            var message = new NotificationMessage
            {
                Type = 0,
                Recipient = user.Email,
                Subject = "Password recovered",
                Body = "Password recovered",
                Priority = 1,
                TemplateName = "Password recovered",
                TemplateParameters = new Dictionary<string, string>
                {
                    { "userName", $"{user.FirstName} {user.LastName}" }
                }
            };
                
            await _notificationService.SendNotificationAsync(message);


            return Result.Ok(new PasswordRecoveryResponse(
                true,
                "Пароль успешно изменен"));
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Error confirming password recovery", ex);
            return Result.Fail("Ошибка при восстановлении пароля");
        }
    }
}
