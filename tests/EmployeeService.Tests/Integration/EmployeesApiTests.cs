using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EmployeeAbsence.Shared.Messaging;
using EmployeeService.Api.Dtos;
using Moq;

namespace EmployeeService.Tests.Integration;

public class EmployeesApiTests(EmployeeApiFactory factory) : IClassFixture<EmployeeApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { companyEmail = email, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    private async Task<(Guid Id, string Token)> CreateEmployeeAndLoginAsync()
    {
        var adminToken = await LoginAsync(EmployeeApiFactory.AdminEmail, EmployeeApiFactory.AdminPassword);
        var email = $"user-{Guid.NewGuid():N}@test.com";

        using var request = new HttpRequestMessage(HttpMethod.Post, "/employees")
        {
            Content = JsonContent.Create(new
            {
                name = "Test User", companyEmail = email, password = "Password1", position = "Staff", phoneNumber = "0811",
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var created = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var employee = await created.Content.ReadFromJsonAsync<EmployeeResponse>();

        return (employee!.Id, await LoginAsync(email, "Password1"));
    }

    private HttpRequestMessage Authorized(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401ProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/auth/login",
            new { companyEmail = EmployeeApiFactory.AdminEmail, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Invalid email or password", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { companyEmail = "not-an-email", password = "x" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEmployees_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/employees");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEmployees_AsEmployee_Returns403()
    {
        var (_, token) = await CreateEmployeeAndLoginAsync();

        var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/employees", token));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEmployees_AsAdmin_Returns200()
    {
        var token = await LoginAsync(EmployeeApiFactory.AdminEmail, EmployeeApiFactory.AdminPassword);

        var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/employees", token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOwnProfile_DoesNotExposePasswordHash()
    {
        var (id, token) = await CreateEmployeeAndLoginAsync();

        var response = await _client.SendAsync(Authorized(HttpMethod.Get, "/employees/me", token));
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(id.ToString(), json);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateOwnProfile_PublishesProfileUpdatedEvent()
    {
        var (id, token) = await CreateEmployeeAndLoginAsync();

        var response = await _client.SendAsync(Authorized(HttpMethod.Put, "/employees/me", token, new { phoneNumber = "0899" }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        factory.Publisher.Verify(p => p.PublishAsync(
            It.Is<EmployeeProfileUpdatedEvent>(e => e.EmployeeId == id && e.ChangedFields["phoneNumber"] == "0899"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateEmployee_WithShortPassword_Returns400()
    {
        var token = await LoginAsync(EmployeeApiFactory.AdminEmail, EmployeeApiFactory.AdminPassword);

        var response = await _client.SendAsync(Authorized(HttpMethod.Post, "/employees", token, new
        {
            name = "X", companyEmail = "x@test.com", password = "short", position = "Staff", phoneNumber = "0811",
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
