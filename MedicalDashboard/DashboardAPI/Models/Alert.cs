using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DashboardAPI.Models;

public class Alert
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid PatientId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string AlertType { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Indicator { get; set; } = string.Empty;
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    public DateTime? AcknowledgedAt { get; set; }
    
    public Guid? AcknowledgedBy { get; set; }
    
    [Required]
    public bool IsProcessed { get; set; } = false;
    
    // Навигационное свойство
    [ForeignKey(nameof(PatientId))]
    public virtual Patient Patient { get; set; } = null!;
} 