using System.ComponentModel.DataAnnotations;
using AbsenceService.Api.Domain;

namespace AbsenceService.Api.Dtos;

/// <summary>
/// Check-in / check-out request. Only <see cref="Status"/> is used: the employee id comes from the JWT
/// and the date/time from the server clock, so users cannot record attendance for others or backdate it.
/// </summary>
public sealed class CreateAbsenceRequest
{
    [Required, RegularExpression("^(masuk|pulang)$", ErrorMessage = "Status must be 'masuk' or 'pulang'.")]
    public string Status { get; set; } = string.Empty;
}

public sealed record AbsenceResponse(Guid Id, Guid EmployeeId, DateOnly Date, TimeOnly Time, string Status, DateTimeOffset CreatedAt)
{
    public static AbsenceResponse From(Absence a) => new(a.Id, a.EmployeeId, a.Date, a.Time, a.Status, a.CreatedAt);
}

/// <summary>One row per day: check-in time (masuk) and check-out time (pulang).</summary>
public sealed record DailyAttendanceSummary(DateOnly Date, TimeOnly? Masuk, TimeOnly? Pulang);
