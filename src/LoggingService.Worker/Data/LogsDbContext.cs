using LoggingService.Worker.Domain;
using Microsoft.EntityFrameworkCore;

namespace LoggingService.Worker.Data;

public class LogsDbContext(DbContextOptions<LogsDbContext> options) : DbContext(options)
{
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(e =>
        {
            e.ToTable("ActivityLogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.Property(x => x.ChangedFields).HasColumnType("nvarchar(max)");
            e.HasIndex(x => new { x.EmployeeId, x.Timestamp });
        });
    }
}
