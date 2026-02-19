using Shared;

namespace DashboardAPI.Services.SignalR;

public interface ISignalRService
{
    Task SendMetricToPatientAsync(Guid patientId, MetricDto metric);
    Task AddToPatientGroupAsync(string connectionId, Guid patientId);
    Task RemoveFromPatientGroupAsync(string connectionId, Guid patientId);
} 