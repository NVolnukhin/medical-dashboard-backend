using Microsoft.EntityFrameworkCore;
using Shared;
namespace DataCollectorService.DCSAppContext
{
    public class DataCollectorDbContext : DbContext
    {
        public DataCollectorDbContext(DbContextOptions<DataCollectorDbContext> options) : base(options)
        {
        }
        public DbSet<PatientDto> Patients { get; set; }
        public DbSet<MetricDto> Metrics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PatientDto>(entity =>
            {
                entity.HasKey(e => e.PatientId);
                entity.ToTable("Patients");
            });

            modelBuilder.Entity<MetricDto>(entity =>
            {
                entity.HasKey(e => e.PatientId);
                entity.ToTable("Metrics");
            });
        }
    }
}
