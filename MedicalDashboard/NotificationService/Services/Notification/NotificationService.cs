using NotificationService.Data.Models;
using NotificationService.Extensions.Logging;
using NotificationService.Interfaces;
using NotificationService.Repositories.Template;

namespace NotificationService.Services.Notification;

public class NotificationService : INotificationService
{
    private readonly IEnumerable<INotificationSender> _senders;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEnumerable<INotificationSender> senders,
        INotificationTemplateRepository templateRepository,
        ILogger<NotificationService> logger)
    {
        _senders = senders;
        _templateRepository = templateRepository;
        _logger = logger;
    }

    public async Task<NotificationResult> SendNotificationAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInfo($"Доступные отправители: {string.Join(", ", _senders.Select(s => s.Type))}");

            var sender = _senders.FirstOrDefault(s => s.Type == request.Type);
            if (sender == null)
            {
                throw new InvalidOperationException($"Отправитель для типа уведомления {request.Type} не найден");
            }

            _logger.LogInfo($"Найден отправитель типа {sender.Type}");

            string subject = request.Subject;
            string body = request.Body;

            // Если указан шаблон, получаем его и подставляем параметры
            if (!string.IsNullOrEmpty(request.TemplateName))
            {
                var template = await _templateRepository.GetBySubjectAndTypeAsync(request.TemplateName, request.Type, cancellationToken);
                if (template == null)
                {
                    throw new InvalidOperationException($"Template '{request.TemplateName}' not found for type {request.Type}");
                }

                // Подставляем параметры в шаблон
                subject = ReplaceTemplateParameters(template.Subject, request.TemplateParameters);
                body = ReplaceTemplateParameters(template.Body, request.TemplateParameters);
                
                _logger.LogInfo($"Использован шаблон '{request.TemplateName}' для уведомления");
            }

            _logger.LogInfo($"Отправка уведомления получателю {request.Recipient}");
            await sender.SendAsync(
                request.Recipient,
                subject,
                body,
                cancellationToken);

            _logger.LogSuccess($"Уведомление успешно отправлено получателю {request.Recipient}");
            return new NotificationResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка отправки уведомления получателю {request.Recipient}", ex);
            return new NotificationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private string ReplaceTemplateParameters(string template, Dictionary<string, string>? parameters)
    {
        if (parameters == null || !parameters.Any())
        {
            return template;
        }

        var result = template;
        foreach (var param in parameters)
        {
            result = result.Replace($"{{{param.Key}}}", param.Value);
        }

        return result;
    }
} 