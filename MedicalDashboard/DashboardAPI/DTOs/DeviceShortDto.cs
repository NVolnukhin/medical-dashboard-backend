namespace DashboardAPI.DTOs;

public class DeviceShortDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> ReadableMetrics { get; set; } = new List<string>();
} 