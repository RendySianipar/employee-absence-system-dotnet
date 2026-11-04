using AbsenceService.Api.Domain;
using AbsenceService.Api.Dtos;
using AbsenceService.Api.Repositories;
using EmployeeAbsence.Shared;
using Microsoft.Extensions.Options;

namespace AbsenceService.Api.Services;

public interface IAbsenceService
{
    Task<AbsenceResponse> RecordAsync(Guid employeeId, string status, CancellationToken cancellationToken = default);
    Task<AbsenceResponse> GetByIdAsync(Guid id, Guid requesterId, bool requesterIsAdmin, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyAttendanceSummary>> GetSummaryAsync(Guid employeeId, DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AbsenceResponse>> GetAllForAdminAsync(DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken = default);
}

public sealed class AbsenceService(IAbsenceRepository repository, TimeProvider timeProvider, IOptions<AttendanceSettings> options)
    : IAbsenceService
{
    private readonly AttendanceSettings _settings = options.Value;
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    /// <summary>
    /// Records a check-in or check-out at the current local time.
    /// Rules: one check-in and one check-out per day; check-out requires a check-in first.
    /// </summary>
    public async Task<AbsenceResponse> RecordAsync(Guid employeeId, string status, CancellationToken cancellationToken = default)
    {
        if (!AttendanceStatus.IsValid(status))
        {
            throw new BusinessValidationException("Status must be 'masuk' or 'pulang'.");
        }

        var now = LocalNow();
        var today = DateOnly.FromDateTime(now);
        var todays = await repository.GetForEmployeeOnDateAsync(employeeId, today, cancellationToken);

        if (todays.Any(a => a.Status == status))
        {
            throw new ConflictException(status == AttendanceStatus.CheckIn
                ? "You have already checked in today."
                : "You have already checked out today.");
        }

        if (status == AttendanceStatus.CheckOut && todays.All(a => a.Status != AttendanceStatus.CheckIn))
        {
            throw new ConflictException("You must check in before checking out.");
        }

        var absence = new Absence
        {
            EmployeeId = employeeId,
            Date = today,
            Time = new TimeOnly(now.Hour, now.Minute, now.Second),
            Status = status,
        };

        await repository.AddAsync(absence, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return AbsenceResponse.From(absence);
    }

    public async Task<AbsenceResponse> GetByIdAsync(Guid id, Guid requesterId, bool requesterIsAdmin, CancellationToken cancellationToken = default)
    {
        var absence = await repository.GetByIdAsync(id, cancellationToken);

        // Non-admins only see their own records; respond 404 (not 403) so ids of others are not revealed.
        if (absence is null || (!requesterIsAdmin && absence.EmployeeId != requesterId))
        {
            throw new NotFoundException($"Absence with id {id} not found");
        }
        return AbsenceResponse.From(absence);
    }

    /// <summary>Daily summary for one employee. Default range: first day of the current month to today.</summary>
    public async Task<IReadOnlyList<DailyAttendanceSummary>> GetSummaryAsync(
        Guid employeeId, DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(LocalNow());
        var start = startDate ?? new DateOnly(today.Year, today.Month, 1);
        var end = endDate ?? today;
        ValidateRange(start, end);

        var records = await repository.GetForEmployeeAsync(employeeId, start, end, cancellationToken);

        return records
            .GroupBy(r => r.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyAttendanceSummary(
                g.Key,
                g.FirstOrDefault(r => r.Status == AttendanceStatus.CheckIn)?.Time,
                g.FirstOrDefault(r => r.Status == AttendanceStatus.CheckOut)?.Time))
            .ToList();
    }

    public async Task<IReadOnlyList<AbsenceResponse>> GetAllForAdminAsync(DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken = default)
    {
        if (startDate is not null && endDate is not null)
        {
            ValidateRange(startDate.Value, endDate.Value);
        }

        var records = await repository.GetAllAsync(startDate, endDate, cancellationToken);
        return records.Select(AbsenceResponse.From).ToList();
    }

    private void ValidateRange(DateOnly start, DateOnly end)
    {
        if (start > end)
        {
            throw new BusinessValidationException("startDate must be on or before endDate.");
        }
        if (end.DayNumber - start.DayNumber > _settings.MaxRangeDays)
        {
            throw new BusinessValidationException($"Date range cannot exceed {_settings.MaxRangeDays} days.");
        }
    }

    private DateTime LocalNow() => TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _timeZone).DateTime;
}
