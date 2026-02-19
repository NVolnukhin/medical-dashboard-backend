using DataAnalysisService.DTOs;
using DataAnalysisService.Services.Alert;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace DataAnalysisService.Data;

public class DataAnalysisDbContext : DbContext
{
    public DataAnalysisDbContext(DbContextOptions<DataAnalysisDbContext> options) : base(options)
    {
    }

    public DbSet<PatientDto> Patients { get; set; }
    public DbSet<AlertDto> Alerts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PatientDto>(entity =>
        {
            entity.HasKey(e => e.PatientId);
            entity.ToTable("Patients");
        });

        modelBuilder.Entity<AlertDto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Alerts");
        });
    }
} 