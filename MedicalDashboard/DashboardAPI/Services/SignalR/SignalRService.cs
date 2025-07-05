using DashboardAPI.Hubs;
using Microsoft.AspNetCore.SignalR;
using Shared;

namespace DashboardAPI.Services.SignalR;

public class SignalRService : ISignalRService
{
    private readonly IHubContext<MetricsHub> _hubContext;

    public SignalRService(IHubContext<MetricsHub> hubContext)
    {
        _hubContext = hubContext;
    }
    
    public async Task SendMetricToPatientAsync(Guid patientId, MetricDto metric)
    {
        var signalRData = new MetricDto
        {
            PatientId = metric.PatientId,
            Value = metric.Value,
            Timestamp = metric.Timestamp,
            Type = metric.Type
        };
    
        await _hubContext.Clients.Group($"patient-{patientId}")
            .SendAsync("ReceiveMetric", signalRData);
    }


    public async Task AddToPatientGroupAsync(string connectionId, Guid patientId)
    {
        await _hubContext.Groups.AddToGroupAsync(connectionId, $"patient-{patientId}");
    }

    public async Task RemoveFromPatientGroupAsync(string connectionId, Guid patientId)
    {
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, $"patient-{patientId}");
    }
} 