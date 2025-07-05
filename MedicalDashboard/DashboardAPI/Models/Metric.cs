using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DashboardAPI.Models;

public class Metric
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid PatientId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    public DateTime Timestamp { get; set; }
    
    [Required]
    public double Value { get; set; }
    
    // Навигационное свойство
    [ForeignKey(nameof(PatientId))]
    public virtual Patient Patient { get; set; } = null!;
} 