using EmployeeAbsence.Shared.Auth;

namespace EmployeeService.Api.Domain;

public class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string Position { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Employee;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
