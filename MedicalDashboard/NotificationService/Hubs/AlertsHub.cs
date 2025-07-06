using Microsoft.AspNetCore.SignalR;
using Shared.Extensions.Logging;

namespace NotificationService.Hubs;

public class AlertsHub : Hub
{
    private readonly ILogger<AlertsHub> _logger;

    public AlertsHub(ILogger<AlertsHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInfo($"SignalR: Новое подключение {Context.ConnectionId}");
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInfo($"SignalR: Отключение {Context.ConnectionId}");
        
        if (exception != null)
        {
            _logger.LogFailure($"SignalR: Ошибка при отключении {Context.ConnectionId}", exception);
        }
        else
        {
            _logger.LogSuccess($"SignalR: Успешное отключение {Context.ConnectionId}");
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}