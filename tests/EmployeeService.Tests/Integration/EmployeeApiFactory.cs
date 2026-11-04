using EmployeeAbsence.Shared.Messaging;
using EmployeeService.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace EmployeeService.Tests.Integration;

/// <summary>Runs the real API in memory with an in-memory database and a mocked RabbitMQ publisher.</summary>
public sealed class EmployeeApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.com";
    public const string AdminPassword = "Admin@12345";

    public Mock<IEventPublisher> Publisher { get; } = new();
    private readonly string _databaseName = $"employees-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Secret", "integration-test-secret-at-least-32-characters");
        builder.UseSetting("SeedAdmin:Email", AdminEmail);
        builder.UseSetting("SeedAdmin:Password", AdminPassword);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<EmployeeDbContext>>();
            services.AddDbContext<EmployeeDbContext>(o => o.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IEventPublisher>();
            services.AddSingleton(Publisher.Object);
        });
    }
}
