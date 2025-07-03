using NotificationService.Data.Models;
using NotificationService.Extensions.Logging;
using NotificationService.Handlers;
using NotificationService.Services.Queue;

namespace NotificationService.Handlers;

public class KafkaNotificationHandler : IMessageHandler<NotificationRequest>
{
    private readonly IPriorityNotificationQueue _queue;
    private readonly ILogger<KafkaNotificationHandler> _logger;

    public KafkaNotificationHandler(
        IPriorityNotificationQueue queue,
        ILogger<KafkaNotificationHandler> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public string Topic => "md-emails";

    public async Task HandleAsync(NotificationRequest message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInfo($"Получено сообщение из Kafka для получателя {message.Recipient} с приоритетом {message.Priority}");

            var request = new NotificationRequest
            {
                Type = message.Type,
                Recipient = message.Recipient,
                Subject = message.Subject,
                Body = message.Body,
                Priority = message.Priority,
                TemplateName = message.TemplateName,
                TemplateParameters = message.TemplateParameters
            };

            // Если используется шаблон, сохраняем его данные в topic и message
            if (!string.IsNullOrEmpty(message.TemplateName))
            {
                request.Topic = $"template-{message.TemplateName}";
                request.Message = System.Text.Json.JsonSerializer.Serialize(new
                {
                    TemplateName = message.TemplateName,
                    TemplateParameters = message.TemplateParameters,
                    OriginalSubject = message.Subject,
                    OriginalBody = message.Body
                });
            }

            await _queue.EnqueueAsync(request);
            _logger.LogSuccess($"Сообщение добавлено в приоритетную очередь. Получатель: {request.Recipient}, Приоритет: {request.Priority}");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка обработки сообщения из Kafka для получателя {message.Recipient}", ex);
            throw;
        }
    }
} 