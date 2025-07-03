using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data.Models;

namespace NotificationService.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Subject).IsRequired();
            entity.Property(e => e.Body).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.RequiredFields)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null))
                .HasColumnType("text");
            
            // Индекс для быстрого поиска по Subject и Type
            entity.HasIndex(e => new { e.Subject, e.Type }).IsUnique();
        });
    }
} 