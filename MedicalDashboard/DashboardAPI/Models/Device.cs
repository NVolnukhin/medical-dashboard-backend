using System.ComponentModel.DataAnnotations;

namespace DashboardAPI.Models;

public class Device
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int Ward { get; set; }

    [Required]
    public bool InUsing { get; set; }

    // Список метрик, которые может считывать аппарат
    [Required]
    public List<string> ReadableMetrics { get; set; } = new List<string>();

    // Пациент, к которому привязан аппарат (может быть null)
    public Guid? BusyBy { get; set; }
} 