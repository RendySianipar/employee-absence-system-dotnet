using AbsenceService.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AbsenceService.Api.Data;

public class AbsenceDbContext(DbContextOptions<AbsenceDbContext> options) : DbContext(options)
{
    public DbSet<Absence> Absences => Set<Absence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Absence>(e =>
        {
            e.ToTable("Absences", t => t.HasCheckConstraint("CK_Absences_Status", "[Status] IN ('masuk', 'pulang')"));
            // Random GUID keys make a poor clustered index (page splits, fragmentation),
            // so the primary key is NONCLUSTERED and rows are clustered by employee + date instead.
            e.HasKey(x => x.Id).IsClustered(false);
            e.Property(x => x.Status).HasMaxLength(10).IsRequired();

            // At most one check-in and one check-out per employee per day.
            // Clustered, so the summary query "WHERE EmployeeId = @id AND Date BETWEEN @start AND @end"
            // is a single range seek over rows stored next to each other.
            e.HasIndex(x => new { x.EmployeeId, x.Date, x.Status }).IsUnique().IsClustered().HasDatabaseName("UX_Absences_EmployeeId_Date_Status");
            // Admin view filters all employees by date range.
            e.HasIndex(x => x.Date).HasDatabaseName("IX_Absences_Date");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<Absence>().Where(e => e.State == EntityState.Added))
        {
            entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
