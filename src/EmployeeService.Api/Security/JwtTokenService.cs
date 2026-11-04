using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeAbsence.Shared.Auth;
using EmployeeService.Api.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeService.Api.Security;

public interface ITokenService
{
    string CreateAccessToken(Employee employee);
}

public sealed class JwtTokenService(IOptions<JwtSettings> options, TimeProvider timeProvider) : ITokenService
{
    private readonly JwtSettings _settings = options.Value;

    public string CreateAccessToken(Employee employee)
    {
        var claims = new[]
        {
            new Claim(AppClaimTypes.Subject, employee.Id.ToString()),
            new Claim(AppClaimTypes.Email, employee.CompanyEmail),
            new Claim(AppClaimTypes.Role, employee.Role),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_settings.ExpiresInMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
