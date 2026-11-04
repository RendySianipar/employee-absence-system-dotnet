using EmployeeAbsence.Shared;
using EmployeeService.Api.Dtos;
using EmployeeService.Api.Repositories;
using EmployeeService.Api.Security;

namespace EmployeeService.Api.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public sealed class AuthService(IEmployeeRepository repository, IPasswordHasher passwordHasher, ITokenService tokenService)
    : IAuthService
{
    private const string InvalidCredentials = "Invalid email or password";

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await repository.GetByEmailAsync(EmailNormalizer.Normalize(request.CompanyEmail), cancellationToken);

        // Same message for unknown email and wrong password, so attackers cannot enumerate accounts.
        if (employee is null || !passwordHasher.Verify(request.Password, employee.PasswordHash))
        {
            throw new UnauthorizedException(InvalidCredentials);
        }

        return new LoginResponse(
            tokenService.CreateAccessToken(employee),
            new LoginEmployee(employee.Id, employee.Name, employee.CompanyEmail, employee.Role));
    }
}
