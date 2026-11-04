namespace LoggingService.Worker.Domain;

public class ActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmployeeId { get; set; }
    public string Action { get; set; } = string.Empty;

    /// <summary>Changed fields serialized as JSON (password values are masked by the publisher).</summary>
    public string? ChangedFields { get; set; }

    /// <summary>When the change happened (from the event).</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>When the log row was written.</summary>
    public DateTimeOffset LoggedAt { get; set; }
}
