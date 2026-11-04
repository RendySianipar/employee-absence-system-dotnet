namespace EmployeeAbsence.Shared.Auth;

/// <summary>Role names stored on the employee record and carried in the JWT "role" claim.</summary>
public static class Roles
{
    public const string Employee = "employee";
    public const string Admin = "admin";

    public static readonly IReadOnlyCollection<string> All = new[] { Employee, Admin };

    public static bool IsValid(string? role) => role is not null && All.Contains(role);
}
