namespace Shared;

public class AlertDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Indicator { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedBy { get; set; }
    public bool IsProcessed { get; set; }
}
