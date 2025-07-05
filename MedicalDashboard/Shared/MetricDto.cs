using System.Text.Json.Serialization;

namespace Shared;

public class MetricDto
{
    [JsonPropertyName("patientId")]
    public Guid PatientId { get; set; }
    [JsonPropertyName("value")]
    public double Value { get; set; }
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;  
}