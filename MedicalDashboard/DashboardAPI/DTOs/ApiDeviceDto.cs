using System.ComponentModel.DataAnnotations;

namespace DashboardAPI.DTOs;

public class ApiDeviceDto
{
    public string Name { get; set; } = string.Empty;
    public int Ward { get; set; }
    public List<string> ReadableMetrics { get; set; } = new List<string>();
} 