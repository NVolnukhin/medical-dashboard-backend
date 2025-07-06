using Shared.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using NotificationService.Enums;
using NotificationService.Hubs;
using NotificationService.Interfaces;

namespace NotificationService.WebPush.Sender;

public class WebPushNotificationSender : INotificationSender
{
    private readonly IHubContext<AlertsHub> _hubContext;
    private readonly ILogger<WebPushNotificationSender> _logger;

    public WebPushNotificationSender(IHubContext<AlertsHub> hubContext, ILogger<WebPushNotificationSender> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public NotificationType Type => NotificationType.WebPush;

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInfo($"Попытка отправки Web Push уведомления через SignalR.");
        _logger.LogInfo($"Размер сообщения: {body?.Length ?? 0} символов");

        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveAlert", body, cancellationToken);
            _logger.LogSuccess($"Web Push уведомление успешно отправлено всем клиентам через SignalR");
        }
        catch (Exception ex)
        {
            _logger.LogFailure("Ошибка при отправке Web Push уведомления через SignalR", ex);
            throw;
        }
    }
}