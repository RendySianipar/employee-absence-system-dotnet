using EmployeeAbsence.Shared;
using EmployeeService.Api.Domain;
using EmployeeService.Api.Dtos;
using EmployeeService.Api.Repositories;
using EmployeeService.Api.Security;
using EmployeeService.Api.Services;
using Moq;

namespace EmployeeService.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IEmployeeRepository> _repository = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly AuthService _sut;

    private readonly Employee _employee = new()
    {
        Id = Guid.NewGuid(),
        Name = "Budi",
        CompanyEmail = "budi@company.com",
        PasswordHash = "hashed",
        Role = "employee",
    };

    public AuthServiceTests()
    {
        _sut = new AuthService(_repository.Object, _hasher.Object, _tokenService.Object);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokenAndEmployee()
    {
        _repository.Setup(r => r.GetByEmailAsync("budi@company.com", It.IsAny<CancellationToken>())).ReturnsAsync(_employee);
        _hasher.Setup(h => h.Verify("secret123", "hashed")).Returns(true);
        _tokenService.Setup(t => t.CreateAccessToken(_employee)).Returns("jwt-token");

        var result = await _sut.LoginAsync(new LoginRequest { CompanyEmail = "budi@company.com", Password = "secret123" });

        Assert.Equal("jwt-token", result.AccessToken);
        Assert.Equal(_employee.Id, result.Employee.Id);
        Assert.Equal("employee", result.Employee.Role);
    }

    [Fact]
    public async Task LoginAsync_NormalizesEmailBeforeLookup()
    {
        _repository.Setup(r => r.GetByEmailAsync("budi@company.com", It.IsAny<CancellationToken>())).ReturnsAsync(_employee);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _tokenService.Setup(t => t.CreateAccessToken(It.IsAny<Employee>())).Returns("jwt-token");

        await _sut.LoginAsync(new LoginRequest { CompanyEmail = "  Budi@Company.COM ", Password = "secret123" });

        _repository.Verify(r => r.GetByEmailAsync("budi@company.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsUnauthorized()
    {
        _repository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Employee?)null);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _sut.LoginAsync(new LoginRequest { CompanyEmail = "nobody@company.com", Password = "x" }));

        Assert.Equal("Invalid email or password", ex.Message);
        _tokenService.Verify(t => t.CreateAccessToken(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsSameMessageAsUnknownEmail()
    {
        _repository.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(_employee);
        _hasher.Setup(h => h.Verify("wrong", "hashed")).Returns(false);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _sut.LoginAsync(new LoginRequest { CompanyEmail = "budi@company.com", Password = "wrong" }));

        Assert.Equal("Invalid email or password", ex.Message);
    }
}
