using System.ComponentModel.DataAnnotations;
using EmployeeService.Api.Domain;

namespace EmployeeService.Api.Dtos;

/// <summary>Employee data returned by the API. The password hash is never exposed.</summary>
public sealed record EmployeeResponse(
    Guid Id,
    string Name,
    string CompanyEmail,
    string? PhotoUrl,
    string Position,
    string PhoneNumber,
    string Role,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static EmployeeResponse From(Employee e) =>
        new(e.Id, e.Name, e.CompanyEmail, e.PhotoUrl, e.Position, e.PhoneNumber, e.Role, e.CreatedAt, e.UpdatedAt);
}

public sealed class CreateEmployeeRequest
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string CompanyEmail { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Position { get; set; } = string.Empty;

    [Required, StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [RegularExpression("^(employee|admin)$", ErrorMessage = "Role must be 'employee' or 'admin'.")]
    public string? Role { get; set; }
}

/// <summary>Admin update: every field is optional; only supplied fields are changed.</summary>
public sealed class UpdateEmployeeRequest
{
    [StringLength(150, MinimumLength = 1)]
    public string? Name { get; set; }

    [EmailAddress, StringLength(256)]
    public string? CompanyEmail { get; set; }

    [MinLength(8)]
    public string? Password { get; set; }

    [StringLength(100, MinimumLength = 1)]
    public string? Position { get; set; }

    [StringLength(30, MinimumLength = 1)]
    public string? PhoneNumber { get; set; }

    [RegularExpression("^(employee|admin)$", ErrorMessage = "Role must be 'employee' or 'admin'.")]
    public string? Role { get; set; }
}

/// <summary>Self-service update by the logged-in employee.</summary>
public sealed class UpdateProfileRequest
{
    [StringLength(500)]
    public string? PhotoUrl { get; set; }

    [StringLength(30, MinimumLength = 1)]
    public string? PhoneNumber { get; set; }

    [MinLength(8)]
    public string? Password { get; set; }
}
