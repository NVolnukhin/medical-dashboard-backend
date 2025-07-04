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
        _logger.LogInfo("Попытка отправки Web Push уведомления через SignalR.");

        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveAlert", body, cancellationToken);
            _logger.LogInfo("Web Push уведомление успешно отправлено всем клиентам.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке Web Push уведомления через SignalR.");
            throw;
        }
    }
}