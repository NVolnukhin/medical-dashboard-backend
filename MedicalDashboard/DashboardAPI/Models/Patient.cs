using System.ComponentModel.DataAnnotations;

namespace DashboardAPI.Models;

public class Patient
{
    [Key]
    public Guid PatientId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? MiddleName { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    public Guid DoctorId { get; set; }
    
    [Required]
    public DateTime BirthDate { get; set; }
    
    [Required]
    [MaxLength(1)]
    public char Sex { get; set; }
    
    public double? Height { get; set; }
    
    public int? Ward { get; set; }
    
    // Навигационные свойства
    public virtual ICollection<Metric> Metrics { get; set; } = new List<Metric>();
    public virtual ICollection<Alert> Alerts { get; set; } = new List<Alert>();
} 