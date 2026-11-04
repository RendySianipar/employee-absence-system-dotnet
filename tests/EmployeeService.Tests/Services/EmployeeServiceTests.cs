using EmployeeAbsence.Shared;
using EmployeeAbsence.Shared.Messaging;
using EmployeeService.Api.Domain;
using EmployeeService.Api.Dtos;
using EmployeeService.Api.Notifications;
using EmployeeService.Api.Repositories;
using EmployeeService.Api.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Sut = EmployeeService.Api.Services.EmployeeService;

namespace EmployeeService.Tests.Services;

public class EmployeeServiceTests
{
    private readonly Mock<IEmployeeRepository> _repository = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly Mock<IAdminNotifier> _notifier = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2026, 11, 4, 2, 0, 0, TimeSpan.Zero));
    private readonly Sut _sut;

    private readonly Employee _employee = new()
    {
        Id = Guid.NewGuid(),
        Name = "Siti",
        CompanyEmail = "siti@company.com",
        PasswordHash = "old-hash",
        Position = "Staff",
        PhoneNumber = "0811",
    };

    public EmployeeServiceTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(p => $"hash({p})");
        _repository.Setup(r => r.GetByIdAsync(_employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_employee);
        _sut = new Sut(_repository.Object, _hasher.Object, _publisher.Object, _notifier.Object, _clock, NullLogger<Sut>.Instance);
    }

    [Fact]
    public async Task CreateAsync_HashesPassword_NormalizesEmail_DefaultsRoleToEmployee()
    {
        Employee? saved = null;
        _repository.Setup(r => r.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Callback<Employee, CancellationToken>((e, _) => saved = e);

        var result = await _sut.CreateAsync(new CreateEmployeeRequest
        {
            Name = "Andi", CompanyEmail = "Andi@Company.com", Password = "Password1", Position = "Dev", PhoneNumber = "0812",
        });

        Assert.NotNull(saved);
        Assert.Equal("hash(Password1)", saved!.PasswordHash);
        Assert.Equal("andi@company.com", result.CompanyEmail);
        Assert.Equal("employee", result.Role);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ThrowsConflict()
    {
        _repository.Setup(r => r.EmailExistsAsync("siti@company.com", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(new CreateEmployeeRequest
        {
            Name = "Siti 2", CompanyEmail = "siti@company.com", Password = "Password1", Position = "Dev", PhoneNumber = "0812",
        }));

        _repository.Verify(r => r.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateOwnProfileAsync_PublishesEventAndNotifiesAdmins_WithMaskedPassword()
    {
        EmployeeProfileUpdatedEvent? published = null;
        _publisher.Setup(p => p.PublishAsync(It.IsAny<EmployeeProfileUpdatedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<EmployeeProfileUpdatedEvent, CancellationToken>((e, _) => published = e)
            .Returns(Task.CompletedTask);

        await _sut.UpdateOwnProfileAsync(_employee.Id, new UpdateProfileRequest { PhoneNumber = "0899", Password = "NewPassword1" });

        Assert.Equal("0899", _employee.PhoneNumber);
        Assert.Equal("hash(NewPassword1)", _employee.PasswordHash);

        Assert.NotNull(published);
        Assert.Equal(EmployeeProfileUpdatedEvent.ProfileUpdatedAction, published!.Action);
        Assert.Equal("Siti", published.EmployeeName);
        Assert.Equal(_clock.GetUtcNow(), published.Timestamp);
        Assert.Equal("0899", published.ChangedFields["phoneNumber"]);
        Assert.Equal(Sut.MaskedValue, published.ChangedFields["password"]);
        Assert.DoesNotContain(published.ChangedFields.Values, v => v != null && v.Contains("NewPassword1"));

        _notifier.Verify(n => n.NotifyProfileUpdatedAsync(published, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOwnProfileAsync_NoActualChange_DoesNotSaveOrPublish()
    {
        await _sut.UpdateOwnProfileAsync(_employee.Id, new UpdateProfileRequest { PhoneNumber = "0811" });

        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _publisher.Verify(p => p.PublishAsync(It.IsAny<EmployeeProfileUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOwnProfileAsync_BrokerDown_StillSucceeds()
    {
        _publisher.Setup(p => p.PublishAsync(It.IsAny<EmployeeProfileUpdatedEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ unreachable"));

        var result = await _sut.UpdateOwnProfileAsync(_employee.Id, new UpdateProfileRequest { PhoneNumber = "0899" });

        Assert.Equal("0899", result.PhoneNumber);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notifier.Verify(n => n.NotifyProfileUpdatedAsync(It.IsAny<EmployeeProfileUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ByAdmin_ChangesRole_AndDoesNotPublishEvent()
    {
        var result = await _sut.UpdateAsync(_employee.Id, new UpdateEmployeeRequest { Role = "admin", Position = "HR Lead" });

        Assert.Equal("admin", result.Role);
        Assert.Equal("HR Lead", result.Position);
        _publisher.Verify(p => p.PublishAsync(It.IsAny<EmployeeProfileUpdatedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EmailTakenByAnotherEmployee_ThrowsConflict()
    {
        _repository.Setup(r => r.EmailExistsAsync("taken@company.com", _employee.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.UpdateAsync(_employee.Id, new UpdateEmployeeRequest { CompanyEmail = "taken@company.com" }));
    }

    [Fact]
    public async Task DeleteAsync_RemovesEmployee()
    {
        await _sut.DeleteAsync(_employee.Id);

        _repository.Verify(r => r.Remove(_employee), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
