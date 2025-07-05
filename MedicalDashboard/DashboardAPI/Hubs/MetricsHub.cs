using DashboardAPI.Services;
using DashboardAPI.Services.SignalR;
using Microsoft.AspNetCore.SignalR;
using Shared.Extensions.Logging;

namespace DashboardAPI.Hubs;

public class MetricsHub : Hub
{
    private readonly ISignalRService _signalRService;
    private readonly ILogger<MetricsHub> _logger;

    public MetricsHub(ISignalRService signalRService, ILogger<MetricsHub> logger)
    {
        _signalRService = signalRService;
        _logger = logger;
    }

    public async Task SubscribeToPatient(Guid patientId)
    {
        _logger.LogInfo($"SignalR: Подписка на пациента {patientId} для соединения {Context.ConnectionId}");
        await _signalRService.AddToPatientGroupAsync(Context.ConnectionId, patientId);
        await Clients.Caller.SendAsync("SubscribedToPatient", patientId);
        _logger.LogSuccess($"SignalR: Успешная подписка на пациента {patientId}");
    }

    public async Task SubscribeToPatientMetrics(Guid patientId)
    {
        _logger.LogInfo($"SignalR: Подписка на метрики пациента {patientId} для соединения {Context.ConnectionId}");
        await _signalRService.AddToPatientGroupAsync(Context.ConnectionId, patientId);
        await Clients.Caller.SendAsync("SubscribedToPatient", patientId);
        _logger.LogSuccess($"SignalR: Успешная подписка на метрики пациента {patientId}");
    }

    public async Task UnsubscribeFromPatient(Guid patientId)
    {
        _logger.LogInfo($"SignalR: Отписка от пациента {patientId} для соединения {Context.ConnectionId}");
        await _signalRService.RemoveFromPatientGroupAsync(Context.ConnectionId, patientId);
        await Clients.Caller.SendAsync("UnsubscribedFromPatient", patientId);
        _logger.LogSuccess($"SignalR: Успешная отписка от пациента {patientId}");
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInfo($"SignalR: Новое подключение {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInfo($"SignalR: Отключение {Context.ConnectionId}. Ошибка: {exception?.Message}");
        await base.OnDisconnectedAsync(exception);
    }
} 