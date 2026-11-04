namespace AbsenceService.Api.Services;

public sealed class AttendanceSettings
{
    public const string SectionName = "Attendance";

    /// <summary>IANA time zone used to decide "today" and the recorded time.</summary>
    public string TimeZone { get; set; } = "Asia/Jakarta";

    /// <summary>Maximum date range (in days) for one summary/admin query.</summary>
    public int MaxRangeDays { get; set; } = 366;
}
