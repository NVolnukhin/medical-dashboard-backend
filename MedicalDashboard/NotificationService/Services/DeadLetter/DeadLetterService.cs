using NotificationService.Data.Models;
using NotificationService.Enums;
using NotificationService.Repositories.DeadLetter;
using Shared.Extensions.Logging;

namespace NotificationService.Services.DeadLetter;

public class DeadLetterService : IDeadLetterService
{
    private readonly IDeadLetterRepository _deadLetterRepository;
    private readonly ILogger<DeadLetterService> _logger;

    public DeadLetterService(
        IDeadLetterRepository repository,
        ILogger<DeadLetterService> logger)
    {
        _deadLetterRepository = repository;
        _logger = logger;
    }

    public async Task PublishToDeadLetterQueueAsync(string topic, string message, string errorMessage, string receiver, CancellationToken cancellationToken = default)
    {
        try
        {
            // Сохраняем напрямую в БД
            var deadLetterMessage = new DeadLetterMessage
            {
                MessageBrokerTopic = topic,
                ErrorMessage = errorMessage,
                Receiver = receiver
            };

            // Пытаемся десериализовать сообщение как NotificationRequest
            try
            {
                var notificationRequest = System.Text.Json.JsonSerializer.Deserialize<NotificationRequest>(message);
                if (notificationRequest != null)
                {
                    if (!string.IsNullOrEmpty(notificationRequest.TemplateName))
                    {
                        // Если есть шаблон, сохраняем его название и параметры
                        deadLetterMessage.Subject = notificationRequest.TemplateName;
                        deadLetterMessage.Body = System.Text.Json.JsonSerializer.Serialize(notificationRequest.TemplateParameters);
                    }
                    else
                    {
                        // Если нет шаблона, сохраняем заголовок и тело
                        deadLetterMessage.Subject = notificationRequest.Subject;
                        deadLetterMessage.Body = notificationRequest.Body;
                    }
                    deadLetterMessage.Priority = notificationRequest.Priority;
                }
            }
            catch
            {
                // Если не удалось десериализовать как NotificationRequest,
                // сохраняем сообщение как есть
                deadLetterMessage.Subject = "Raw Message";
                deadLetterMessage.Body = message;
                deadLetterMessage.Priority = NotificationPriority.Normal;
            }

            await _deadLetterRepository.AddAsync(deadLetterMessage, cancellationToken);
            _logger.LogSuccess($"Сообщение успешно добавлено в Dead Letter Queue для получателя {receiver}");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка сохранения 'мертвого' сообщения", ex);
            throw;
        }
    }

    public async Task<IEnumerable<DeadLetterMessage>> GetAllDeadLettersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _deadLetterRepository.GetAllAsync(cancellationToken);
            var allDeadLettersAsync = messages.ToList();
            _logger.LogSuccess($"Успешно получены все сообщения из Dead Letter Queue. Count: {allDeadLettersAsync.Count}");
            return allDeadLettersAsync;
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка получения всех сообщений из Dead Letter Queue", ex);
            throw;
        }
    }

    public async Task<IEnumerable<DeadLetterMessage>> GetUnprocessedDeadLettersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = await _deadLetterRepository.GetUnprocessedAsync(cancellationToken);
            var unprocessedDeadLettersAsync = messages.ToList();
            _logger.LogSuccess($"Успешно получены необработанные сообщения из Dead Letter Queue. Count: {unprocessedDeadLettersAsync.Count}");
            return unprocessedDeadLettersAsync;
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка получения необработанных сообщений из Dead Letter Queue", ex);
            throw;
        }
    }

    public async Task<DeadLetterMessage> ProcessDeadLetterAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _deadLetterRepository.MarkAsProcessedAsync(id, cancellationToken);
            _logger.LogSuccess($"Сообщение {id} успешно отмечено как обработанное");
            return message;
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning($"Сообщение {id} не найдено");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка обработки сообщения {id}", ex);
            throw;
        }
    }
} 