using System.Text.Json;
using NotificationService.Data.Models;
using NotificationService.Enums;
using NotificationService.Services.Notification;
using Shared;
using Shared.Extensions.Logging;

namespace NotificationService.Handlers;

public class PatientAlertMessageHandler : IMessageHandler<PatientAlertMessage>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<PatientAlertMessageHandler> _logger;

    public string Topic => "md-alerts";

    public PatientAlertMessageHandler(INotificationService notificationService, ILogger<PatientAlertMessageHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(PatientAlertMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInfo($"Обработка сообщения для пациента: {message.PatientName} ({message.PatientId})");
        _logger.LogInfo($"Тип алерта: {message.AlertType}");
        _logger.LogInfo($"Индикатор: {message.Indicator}");

        var messageBody = JsonSerializer.Serialize(message);
        _logger.LogInfo($"Сериализованное сообщение: {messageBody}");

        var notificationRequest = new NotificationRequest
        {
            Recipient = message.PatientId,
            Subject = $"Оповещение: {message.AlertType}",
            Body = messageBody,
            Type = NotificationType.WebPush
        };

        _logger.LogInfo($"Создан запрос на уведомление типа {notificationRequest.Type}");
        await _notificationService.SendNotificationAsync(notificationRequest, cancellationToken);
        _logger.LogSuccess($"Уведомление успешно обработано для пациента {message.PatientId}");
    }
}