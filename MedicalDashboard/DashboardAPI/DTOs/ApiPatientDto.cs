using System.ComponentModel.DataAnnotations;

namespace DashboardAPI.DTOs;

public class ApiPatientDto
{
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public DateTime BirthDate { get; set; }
    public char Sex { get; set; }
    public double? Height { get; set; }
    public int? Ward { get; set; }
}
