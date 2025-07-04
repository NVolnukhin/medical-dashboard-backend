using AuthService.DTOs.PasswordRecovery;
using AuthService.Services.PasswordRecovery;
using Microsoft.AspNetCore.Mvc;
using Shared.Extensions.Logging;

namespace AuthService.Controllers;

[ApiController]
[Route("api/password-recovery")]
public class PasswordRecoveryController : ControllerBase
{
    private readonly IPasswordRecoveryService _recoveryService;
    private readonly ILogger<PasswordRecoveryController> _logger;

    public PasswordRecoveryController(
        IPasswordRecoveryService recoveryService,
        ILogger<PasswordRecoveryController> logger)
    {
        _recoveryService = recoveryService;
        _logger = logger;
    }

    [HttpPost("request")]
    public async Task<IActionResult> RequestRecovery(
        [FromBody] PasswordRecoveryRequest request)
    {
        try
        {
            var result = await _recoveryService.RequestRecoveryAsync(request.Email);
            
            Console.WriteLine($"Восстановление пароля для {request.Email}");
            
            if (!result.IsSuccess)
            {
                return BadRequest("Ошибка при запросе восстановления пароля");
            }
            
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Необработанная ошибка при восстановлении пароля", ex);
            return StatusCode(500, "Произошла непредвиденная ошибка");
        }
    }
    
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmRecovery(
        [FromBody] PasswordRecoveryConfirm request)
    {
        _logger.LogInfo($"Запрос на восстановление пароля: {request}");
        try
        {
            var result = await _recoveryService.ConfirmRecoveryAsync(request);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Service errors: {Errors}", string.Join(", ", result.Errors));
            }
            
            return result.IsSuccess 
                ? Ok(result.Value) 
                : BadRequest("Ошибка при подтверждении восстановления");
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка восстановления пароля", ex);
            return StatusCode(500, "Произошла непредвиденная ошибка");
        }
    }
}