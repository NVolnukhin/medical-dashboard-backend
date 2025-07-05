namespace Shared;

public class AlertMessage
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty; // "alert" или "warning"
    public string MetricType { get; set; } = string.Empty; // например "pulse"
} 