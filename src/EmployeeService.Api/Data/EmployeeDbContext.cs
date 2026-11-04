using EmployeeService.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace EmployeeService.Api.Data;

public class EmployeeDbContext(DbContextOptions<EmployeeDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(e =>
        {
            e.ToTable("Employees");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.CompanyEmail).HasMaxLength(256).IsRequired();
            e.HasIndex(x => x.CompanyEmail).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(100).IsRequired();
            e.Property(x => x.PhotoUrl).HasMaxLength(500);
            e.Property(x => x.Position).HasMaxLength(100).IsRequired();
            e.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Employee>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
