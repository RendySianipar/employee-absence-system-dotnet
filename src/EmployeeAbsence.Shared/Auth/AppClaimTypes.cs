namespace EmployeeAbsence.Shared.Auth;

/// <summary>Short JWT claim names (kept identical to the original NestJS token payload).</summary>
public static class AppClaimTypes
{
    public const string Subject = "sub";
    public const string Email = "email";
    public const string Role = "role";
}
