using EmployeeAbsence.Shared;
using EmployeeAbsence.Shared.Auth;
using EmployeeAbsence.Shared.Messaging;
using EmployeeService.Api.Domain;
using EmployeeService.Api.Dtos;
using EmployeeService.Api.Notifications;
using EmployeeService.Api.Repositories;
using EmployeeService.Api.Security;

namespace EmployeeService.Api.Services;

public interface IEmployeeService
{
    Task<IReadOnlyList<EmployeeResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<EmployeeResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeResponse> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeResponse> UpdateOwnProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class EmployeeService(
    IEmployeeRepository repository,
    IPasswordHasher passwordHasher,
    IEventPublisher eventPublisher,
    IAdminNotifier adminNotifier,
    TimeProvider timeProvider,
    ILogger<EmployeeService> logger) : IEmployeeService
{
    /// <summary>Value written to logs/notifications instead of the real password.</summary>
    public const string MaskedValue = "********";

    public async Task<IReadOnlyList<EmployeeResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await repository.GetAllAsync(cancellationToken)).Select(EmployeeResponse.From).ToList();

    public async Task<EmployeeResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        EmployeeResponse.From(await FindOrThrowAsync(id, cancellationToken));

    public async Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var email = EmailNormalizer.Normalize(request.CompanyEmail);
        if (await repository.EmailExistsAsync(email, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"Company email {email} is already registered");
        }

        var employee = new Employee
        {
            Name = request.Name.Trim(),
            CompanyEmail = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Position = request.Position.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Role = request.Role ?? Roles.Employee,
        };

        await repository.AddAsync(employee, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return EmployeeResponse.From(employee);
    }

    /// <summary>Admin update of any field. Does not publish a profile-updated event (same as the original spec).</summary>
    public async Task<EmployeeResponse> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await FindOrThrowAsync(id, cancellationToken);

        if (request.CompanyEmail is not null)
        {
            var email = EmailNormalizer.Normalize(request.CompanyEmail);
            if (await repository.EmailExistsAsync(email, excludeId: id, cancellationToken))
            {
                throw new ConflictException($"Company email {email} is already registered");
            }
            employee.CompanyEmail = email;
        }

        if (request.Name is not null) employee.Name = request.Name.Trim();
        if (request.Position is not null) employee.Position = request.Position.Trim();
        if (request.PhoneNumber is not null) employee.PhoneNumber = request.PhoneNumber.Trim();
        if (request.Role is not null) employee.Role = request.Role;
        if (request.Password is not null) employee.PasswordHash = passwordHasher.Hash(request.Password);

        await repository.SaveChangesAsync(cancellationToken);
        return EmployeeResponse.From(employee);
    }

    /// <summary>
    /// Employee self-service update. Publishes an event to RabbitMQ (audit log)
    /// and pushes a real-time notification to admins. The password is masked.
    /// </summary>
    public async Task<EmployeeResponse> UpdateOwnProfileAsync(Guid id, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await FindOrThrowAsync(id, cancellationToken);
        var changed = new Dictionary<string, string?>();

        if (request.PhoneNumber is not null && request.PhoneNumber.Trim() != employee.PhoneNumber)
        {
            employee.PhoneNumber = request.PhoneNumber.Trim();
            changed["phoneNumber"] = employee.PhoneNumber;
        }

        if (request.PhotoUrl is not null && request.PhotoUrl != employee.PhotoUrl)
        {
            employee.PhotoUrl = request.PhotoUrl;
            changed["photoUrl"] = employee.PhotoUrl;
        }

        if (request.Password is not null)
        {
            employee.PasswordHash = passwordHasher.Hash(request.Password);
            changed["password"] = MaskedValue;
        }

        if (changed.Count == 0)
        {
            return EmployeeResponse.From(employee);
        }

        await repository.SaveChangesAsync(cancellationToken);

        var payload = new EmployeeProfileUpdatedEvent(
            employee.Id,
            employee.Name,
            EmployeeProfileUpdatedEvent.ProfileUpdatedAction,
            changed,
            timeProvider.GetUtcNow());

        // The profile is already saved; a broker or socket outage must not fail the user's request.
        try
        {
            await eventPublisher.PublishAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish profile-updated event for employee {EmployeeId}", employee.Id);
        }

        try
        {
            await adminNotifier.NotifyProfileUpdatedAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to notify admins about employee {EmployeeId}", employee.Id);
        }

        return EmployeeResponse.From(employee);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await FindOrThrowAsync(id, cancellationToken);
        repository.Remove(employee);
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Employee> FindOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Employee with id {id} not found");
}
