namespace EmployeeAbsence.Shared.Auth;

/// <summary>
/// JWT options. The same Secret/Issuer/Audience must be configured in every service,
/// because Employee Service issues the token and the other services only validate it.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "employee-service";
    public string Audience { get; set; } = "employee-absence-system";
    public int ExpiresInMinutes { get; set; } = 60;
}
