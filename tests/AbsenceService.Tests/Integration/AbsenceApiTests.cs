using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using AbsenceService.Api.Data;
using AbsenceService.Api.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace AbsenceService.Tests.Integration;

public sealed class AbsenceApiFactory : WebApplicationFactory<Program>
{
    public const string Secret = "integration-test-secret-at-least-32-characters";
    private readonly string _databaseName = $"absences-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:Secret", Secret);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AbsenceDbContext>>();
            services.AddDbContext<AbsenceDbContext>(o => o.UseInMemoryDatabase(_databaseName));
        });
    }

    /// <summary>Creates a token the same way Employee Service does (shared secret/issuer/audience).</summary>
    public static string CreateToken(Guid employeeId, string role, string? secret = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret ?? Secret));
        var token = new JwtSecurityToken(
            issuer: "employee-service",
            audience: "employee-absence-system",
            claims: [new Claim("sub", employeeId.ToString()), new Claim("role", role)],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class AbsenceApiTests(AbsenceApiFactory factory) : IClassFixture<AbsenceApiFactory>
{
    private HttpClient ClientFor(Guid employeeId, string role = "employee")
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AbsenceApiFactory.CreateToken(employeeId, role));
        return client;
    }

    [Fact]
    public async Task CheckIn_ThenSummary_ReturnsTodayWithMasukTime()
    {
        var employeeId = Guid.NewGuid();
        var client = ClientFor(employeeId);

        var checkIn = await client.PostAsJsonAsync("/absences", new { status = "masuk" });
        Assert.Equal(HttpStatusCode.Created, checkIn.StatusCode);

        var summary = await client.GetFromJsonAsync<List<DailyAttendanceSummary>>("/absences/summary");
        var today = Assert.Single(summary!);
        Assert.NotNull(today.Masuk);
        Assert.Null(today.Pulang);
    }

    [Fact]
    public async Task CheckIn_IgnoresEmployeeIdFromBody_UsesTokenSubject()
    {
        var employeeId = Guid.NewGuid();
        var client = ClientFor(employeeId);

        var response = await client.PostAsJsonAsync("/absences", new { status = "masuk", employeeId = Guid.NewGuid() });
        var created = await response.Content.ReadFromJsonAsync<AbsenceResponse>();

        Assert.Equal(employeeId, created!.EmployeeId);
    }

    [Fact]
    public async Task CheckInTwice_Returns409()
    {
        var client = ClientFor(Guid.NewGuid());
        await client.PostAsJsonAsync("/absences", new { status = "masuk" });

        var second = await client.PostAsJsonAsync("/absences", new { status = "masuk" });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task InvalidStatus_Returns400()
    {
        var response = await ClientFor(Guid.NewGuid()).PostAsJsonAsync("/absences", new { status = "izin" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_AsEmployee_Returns403_AsAdmin_Returns200()
    {
        var asEmployee = await ClientFor(Guid.NewGuid()).GetAsync("/absences/admin");
        var asAdmin = await ClientFor(Guid.NewGuid(), "admin").GetAsync("/absences/admin");

        Assert.Equal(HttpStatusCode.Forbidden, asEmployee.StatusCode);
        Assert.Equal(HttpStatusCode.OK, asAdmin.StatusCode);
    }

    [Fact]
    public async Task TokenSignedWithDifferentSecret_Returns401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            AbsenceApiFactory.CreateToken(Guid.NewGuid(), "admin", secret: "some-other-secret-that-is-32-chars-long!!"));

        var response = await client.GetAsync("/absences/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_OtherEmployeesRecord_Returns404()
    {
        var owner = ClientFor(Guid.NewGuid());
        var created = await (await owner.PostAsJsonAsync("/absences", new { status = "masuk" })).Content.ReadFromJsonAsync<AbsenceResponse>();

        var response = await ClientFor(Guid.NewGuid()).GetAsync($"/absences/{created!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
