using System.Security.Claims;

namespace EmployeeAbsence.Shared.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns the employee id from the "sub" claim of the authenticated user.</summary>
    public static Guid GetEmployeeId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(AppClaimTypes.Subject);
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedException("Token does not contain a valid employee id.");
    }

    public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);
}
