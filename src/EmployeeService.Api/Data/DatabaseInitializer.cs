using EmployeeAbsence.Shared.Auth;
using EmployeeService.Api.Domain;
using EmployeeService.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace EmployeeService.Api.Data;

public sealed class SeedAdminSettings
{
    public const string SectionName = "SeedAdmin";
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string Name { get; set; } = "HR Administrator";
}

/// <summary>Applies EF Core migrations and creates the first admin account if configured.</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        if (db.Database.IsRelational())
        {
            // SQL Server in Docker can take a few seconds to accept connections.
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await db.Database.MigrateAsync(cancellationToken);
                    break;
                }
                catch (Exception ex) when (attempt < 10)
                {
                    logger.LogWarning(ex, "Database not ready (attempt {Attempt}), retrying in 5s", attempt);
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
        }

        var seed = scope.ServiceProvider.GetRequiredService<IConfiguration>()
            .GetSection(SeedAdminSettings.SectionName).Get<SeedAdminSettings>();
        if (string.IsNullOrWhiteSpace(seed?.Email) || string.IsNullOrWhiteSpace(seed.Password))
        {
            return;
        }

        var email = seed.Email.Trim().ToLowerInvariant();
        if (await db.Employees.AnyAsync(e => e.CompanyEmail == email, cancellationToken))
        {
            return;
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        db.Employees.Add(new Employee
        {
            Name = seed.Name,
            CompanyEmail = email,
            PasswordHash = hasher.Hash(seed.Password),
            Position = "HR",
            PhoneNumber = "-",
            Role = Roles.Admin,
        });
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded admin account {Email}", email);
    }
}
