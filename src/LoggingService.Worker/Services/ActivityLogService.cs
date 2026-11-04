using System.Text.Json;
using EmployeeAbsence.Shared.Messaging;
using LoggingService.Worker.Data;
using LoggingService.Worker.Domain;

namespace LoggingService.Worker.Services;

public interface IActivityLogService
{
    Task<ActivityLog> LogProfileUpdatedAsync(EmployeeProfileUpdatedEvent message, CancellationToken cancellationToken = default);
}

public sealed class ActivityLogService(LogsDbContext db, TimeProvider timeProvider) : IActivityLogService
{
    public async Task<ActivityLog> LogProfileUpdatedAsync(EmployeeProfileUpdatedEvent message, CancellationToken cancellationToken = default)
    {
        var log = new ActivityLog
        {
            EmployeeId = message.EmployeeId,
            Action = message.Action,
            ChangedFields = JsonSerializer.Serialize(message.ChangedFields, MessageSerializer.Options),
            Timestamp = message.Timestamp,
            LoggedAt = timeProvider.GetUtcNow(),
        };

        db.ActivityLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
        return log;
    }
}
