using System.Security.Claims;
using AuthService.DTOs;
using AuthService.Kafka;
using AuthService.Models;
using AuthService.Services.Identity;
using AuthService.Services.Jwt;
using AuthService.Services.Password;
using AuthService.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Extensions.Logging;

namespace AuthService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IdentityController : ControllerBase
    {
        private readonly IIdentityService _identityService;
        private readonly IJwtBuilder _jwtBuilder;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<PasswordRecoveryController> _logger;
        private readonly IKafkaProducerService _notificationService;
        private readonly IOneTimePasswordGenerator _oneTimePasswordGenerator;
        private readonly IUserService _userService;

        public IdentityController(
            IIdentityService identityService, 
            IJwtBuilder jwtBuilder, 
            IPasswordService passwordService,
            ILogger<PasswordRecoveryController> logger,
            IKafkaProducerService notificationService,
            IOneTimePasswordGenerator oneTimePasswordGenerator,
            IUserService userService) 
        {
            _identityService = identityService;
            _jwtBuilder = jwtBuilder;
            _passwordService = passwordService;
            _logger = logger;
            _notificationService = notificationService;
            _oneTimePasswordGenerator = oneTimePasswordGenerator;
            _userService = userService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _identityService.GetUserAsync(request.Email);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var response = await _identityService.LoginAsync(
                    request.Email, 
                    request.Password, 
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                return Ok(new LoginResponse
                {
                    AccessToken = response.AccessToken,
                    Status = response.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogFailure("Ошибка входа", ex);
                return BadRequest(new { message = "Could not authenticate user." });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var u = await _identityService.GetUserAsync(request.Email);

                // проверка есть пользователь уже в сервисе 
                if (u != null)
                {
                    return BadRequest("User already exists.");
                }
                // генерация пароля для пользователя 
                var password = _oneTimePasswordGenerator.GeneratePassword(12);
                var (passwordHash, salt) = _passwordService.CreatePasswordHash(password);

                // создание пользователя
                var user = new User
                {
                    Email = request.Email,
                    Password = passwordHash,
                    Salt = salt,
                    IsActive = true,
                    FirstName = request.FirstName,
                    MiddleName = request.MiddleName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber
                };

                await _identityService.InsertUserAsync(user);
                
                // отправка письма на почту 
                var message = new NotificationMessage
                {
                    Type = 0,
                    Recipient = request.Email,
                    Subject = "Приветственное письмо",
                    Body = "Приветственное письмо",
                    Priority = 0,
                    TemplateName = "Приветственное письмо",
                    TemplateParameters = new Dictionary<string, string>
                    {
                        { "userName", $"{request.FirstName} {request.LastName}" },
                        { "login", request.Email },
                        { "password", password }
                    }
                };

                await _notificationService.SendNotificationAsync(message);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogFailure("Необработанная ошибка во время регистрации", ex);
                return StatusCode(500, "Произошла непредвиденная ошибка");
            }
        }


        [HttpGet("validate")]
        public async Task<IActionResult> Validate(string email, string token)
        {
            try
            {
                var u = await _identityService.GetUserAsync(email);

                if (u == null)
                {
                    return NotFound("User not found.");
                }

                var userId = _jwtBuilder.ValidateToken(token);

                if (Guid.Parse(userId) != u.Id)
                {
                    return BadRequest("Invalid token.");
                }

                return Ok(userId);
            }
            catch (Exception ex)
            {
                _logger.LogFailure("Необработанная ошибка во время валидации логина", ex);
                return StatusCode(500, "Произошла непредвиденная ошибка");
            }
        }
        
        [HttpPut("update-password")]
        [Authorize]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            try
            {
                //получаем userId из клеймов и пытаемся обновть пароль
                _logger.LogInfo($"смены пароля");
                var userId = GetUserIdFromClaims();
                var result = await _userService.UpdatePassword(userId, request);
                
                return result.IsFailed ? BadRequest(result.Errors) : Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        private Guid GetUserIdFromClaims()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "userId" || c.Type == ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInfo($"Получен userId из клеймов для смены пароля: {userId}");
            return Guid.Parse(userId ?? throw new UnauthorizedAccessException("userId не найден в клеймах"));
        }
    }
}
