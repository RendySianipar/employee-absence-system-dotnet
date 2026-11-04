namespace AbsenceService.Api.Domain;

/// <summary>Values kept identical to the original system ("masuk" = check in, "pulang" = check out).</summary>
public static class AttendanceStatus
{
    public const string CheckIn = "masuk";
    public const string CheckOut = "pulang";

    public static bool IsValid(string? status) => status is CheckIn or CheckOut;
}

public class Absence
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public string Status { get; set; } = AttendanceStatus.CheckIn;
    public DateTimeOffset CreatedAt { get; set; }
}
