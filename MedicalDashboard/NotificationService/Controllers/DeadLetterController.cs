using Microsoft.AspNetCore.Mvc;
using NotificationService.Data.Models;
using NotificationService.Services.DeadLetter;

namespace NotificationService.Controllers;

/// <summary>
/// Контроллер для работы с сообщениями в очереди Dead Letter
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DeadLetterController : ControllerBase
{
    private readonly IDeadLetterService _deadLetterService;
    private readonly ILogger<DeadLetterController> _logger;

    public DeadLetterController(
        IDeadLetterService deadLetterService,
        ILogger<DeadLetterController> logger)
    {
        _deadLetterService = deadLetterService;
        _logger = logger;
    }

    /// <summary>
    /// Получает список всех сообщений в очереди Dead Letter
    /// </summary>
    /// <returns>Список всех сообщений в очереди Dead Letter</returns>
    /// <response code="200">Возвращает список сообщений</response>
    /// <response code="500">Если произошла внутренняя ошибка сервера</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DeadLetterMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(NotificationResult), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DeadLetterMessage>>> GetAllDeadLetters()
    {
        try
        {
            var deadLetters = await _deadLetterService.GetAllDeadLettersAsync();
            return Ok(deadLetters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all dead letter messages");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new NotificationResult { Success = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Получает список необработанных сообщений в очереди Dead Letter
    /// </summary>
    /// <returns>Список необработанных сообщений в очереди Dead Letter</returns>
    /// <response code="200">Возвращает список необработанных сообщений</response>
    /// <response code="500">Если произошла внутренняя ошибка сервера</response>
    [HttpGet("unprocessed")]
    [ProducesResponseType(typeof(IEnumerable<DeadLetterMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(NotificationResult), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DeadLetterMessage>>> GetUnprocessedDeadLetters()
    {
        try
        {
            var deadLetters = await _deadLetterService.GetUnprocessedDeadLettersAsync();
            return Ok(deadLetters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unprocessed dead letter messages");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new NotificationResult { Success = false, ErrorMessage = ex.Message });
        }
    }

    /// <summary>
    /// Отмечает сообщение как обработанное
    /// </summary>
    /// <param name="id">Идентификатор сообщения</param>
    /// <returns>Обработанное сообщение</returns>
    /// <response code="200">Сообщение успешно обработано</response>
    /// <response code="404">Сообщение не найдено</response>
    /// <response code="500">Если произошла внутренняя ошибка сервера</response>
    [HttpPost("{id:guid}/process")]
    [ProducesResponseType(typeof(DeadLetterMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(NotificationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(NotificationResult), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeadLetterMessage>> ProcessDeadLetter(Guid id)
    {
        try
        {
            var message = await _deadLetterService.ProcessDeadLetterAsync(id);
            return Ok(message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new NotificationResult 
            { 
                Success = false, 
                ErrorMessage = $"Dead letter message with id {id} not found" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing dead letter message {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new NotificationResult { Success = false, ErrorMessage = ex.Message });
        }
    }
} 