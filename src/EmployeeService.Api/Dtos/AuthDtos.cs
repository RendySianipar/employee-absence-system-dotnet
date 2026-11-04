using System.ComponentModel.DataAnnotations;

namespace EmployeeService.Api.Dtos;

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string CompanyEmail { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public sealed record LoginEmployee(Guid Id, string Name, string CompanyEmail, string Role);

public sealed record LoginResponse(string AccessToken, LoginEmployee Employee);
