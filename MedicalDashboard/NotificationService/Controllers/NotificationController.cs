using Microsoft.AspNetCore.Mvc;
using NotificationService.Data.Models;
using NotificationService.Enums;
using NotificationService.Extensions.Logging;
using NotificationService.Services.Queue;

namespace NotificationService.Controllers;

/// <summary>
/// Контроллер для отправки уведомлений
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NotificationController : ControllerBase
{
    private readonly IPriorityNotificationQueue _queue;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        IPriorityNotificationQueue queue,
        ILogger<NotificationController> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    /// <summary>
    /// Отправляет уведомление получателю
    /// </summary>
    /// <param name="request">Данные для отправки уведомления</param>
    /// <returns>Результат операции</returns>
    /// <response code="200">Уведомление успешно отправлено</response>
    /// <response code="400">Некорректные данные запроса</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpPost("notify")]
    [ProducesResponseType(typeof(NotificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(NotificationResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(NotificationResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Notify([FromBody] NotificationRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Некорректные данные запроса при отправке уведомления");
                return BadRequest(new NotificationResult 
                { 
                    Success = false, 
                    ErrorMessage = "Некорректные данные запроса" 
                });
            }

            _logger.LogInfo($"Получен запрос на отправку уведомления получателю {request.Recipient} с приоритетом {request.Priority}");

            await _queue.EnqueueAsync(request);
            
            _logger.LogSuccess($"Сообщение добавлено в приоритетную очередь. Получатель: {request.Recipient}, Приоритет: {request.Priority}");
            
            return Ok(new NotificationResult { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при добавлении сообщения в очередь", ex);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new NotificationResult { Success = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Получить все типы уведомлений
    /// </summary>
    /// <returns>Список типов уведомлений</returns>
    /// <response code="200">Возвращает список типов уведомлений</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet("types")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(NotificationResult), StatusCodes.Status500InternalServerError)]
    public IActionResult GetNotificationTypes()
    {
        try
        {
            var types = Enum.GetValues(typeof(NotificationType))
                .Cast<NotificationType>()
                .Select(t => new { Name = t.ToString(), Value = (int)t })
                .ToList();
            return Ok(types);
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка при получении типов уведомлений", ex);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new NotificationResult { Success = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Получить все типы приоритетов
    /// </summary>
    /// <returns>Список типов приоритетов</returns>
    /// <response code="200">Возвращает список типов приоритетов</response>
    /// <response code="500">Внутренняя ошибка сервера</response>
    [HttpGet("priorities")]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(NotificationResult), StatusCodes.Status500InternalServerError)]
    public IActionResult GetNotificationPriorities()
    {
        try
        {
            var priorities = Enum.GetValues(typeof(NotificationPriority))
                .Cast<NotificationPriority>()
                .Select(p => new { Name = p.ToString(), Value = (int)p })
                .ToList();
            return Ok(priorities);
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка при получении типов приоритетов", ex);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new NotificationResult { Success = false, ErrorMessage = ex.Message });
        }
    }
}