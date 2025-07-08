namespace DashboardAPI.DTOs;

public class DeviceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Ward { get; set; }
    public bool InUsing { get; set; }
    public List<string> ReadableMetrics { get; set; } = new List<string>();
    public Guid? BusyBy { get; set; }
} 