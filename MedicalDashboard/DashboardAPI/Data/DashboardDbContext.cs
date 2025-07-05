using DashboardAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DashboardAPI.Data;

public class DashboardDbContext : DbContext
{
    public DashboardDbContext(DbContextOptions<DashboardDbContext> options) : base(options)
    {
    }
    
    public DbSet<Patient> Patients { get; set; } = null!;
    public DbSet<Metric> Metrics { get; set; } = null!;
    public DbSet<Alert> Alerts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Конфигурация Patient
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.PatientId);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DoctorId).IsRequired();
            entity.Property(e => e.BirthDate).IsRequired();
            entity.Property(e => e.Sex).IsRequired().HasMaxLength(1);
            entity.Property(e => e.Height);
            entity.Property(e => e.Ward);
            
            // Индексы для быстрого поиска
            entity.HasIndex(e => e.DoctorId);
            entity.HasIndex(e => e.Ward);
            entity.HasIndex(e => new { e.FirstName, e.LastName });
        });

        // Конфигурация Metric
        modelBuilder.Entity<Metric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PatientId).IsRequired();
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Value).IsRequired();
            
            // Связь с Patient
            entity.HasOne(e => e.Patient)
                .WithMany(p => p.Metrics)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Индексы для быстрого поиска
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.PatientId, e.Type, e.Timestamp });
        });

        // Конфигурация Alert
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PatientId).IsRequired();
            entity.Property(e => e.AlertType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Indicator).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.AcknowledgedAt);
            entity.Property(e => e.AcknowledgedBy);
            entity.Property(e => e.IsProcessed).IsRequired();
            
            // Связь с Patient
            entity.HasOne(e => e.Patient)
                .WithMany(p => p.Alerts)
                .HasForeignKey(e => e.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Индексы для быстрого поиска
            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsProcessed);
        });
    }
} 