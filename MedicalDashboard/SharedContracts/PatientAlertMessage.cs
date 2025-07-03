using System.Text.Json.Serialization;

namespace NotificationService.Data.Models;

public class PatientAlertMessage
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("patientName")]
    public string PatientName { get; set; } = string.Empty;

    [JsonPropertyName("alertType")]
    public string AlertType { get; set; } = string.Empty;

    [JsonPropertyName("indicator")]
    public string Indicator { get; set; } = string.Empty;
}