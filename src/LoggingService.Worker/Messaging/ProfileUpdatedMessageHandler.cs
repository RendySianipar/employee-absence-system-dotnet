using System.Text.Json;
using EmployeeAbsence.Shared.Messaging;
using LoggingService.Worker.Services;

namespace LoggingService.Worker.Messaging;

public enum HandleResult
{
    /// <summary>Processed; acknowledge the message.</summary>
    Success,
    /// <summary>Message is malformed and will never succeed; reject without requeue.</summary>
    Discard,
}

/// <summary>Turns a raw RabbitMQ body into an activity log row. Kept separate from the consumer so it can be unit tested.</summary>
public sealed class ProfileUpdatedMessageHandler(IActivityLogService activityLogService, ILogger<ProfileUpdatedMessageHandler> logger)
{
    public async Task<HandleResult> HandleAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken = default)
    {
        EmployeeProfileUpdatedEvent? message;
        try
        {
            message = MessageSerializer.Deserialize<EmployeeProfileUpdatedEvent>(body.Span);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Discarding message that is not valid JSON");
            return HandleResult.Discard;
        }

        if (message is null || message.EmployeeId == Guid.Empty || string.IsNullOrWhiteSpace(message.Action))
        {
            logger.LogWarning("Discarding message without employeeId/action");
            return HandleResult.Discard;
        }

        // Database errors bubble up so the consumer can requeue the message.
        await activityLogService.LogProfileUpdatedAsync(message, cancellationToken);
        logger.LogInformation("Logged {Action} for employee {EmployeeId}", message.Action, message.EmployeeId);
        return HandleResult.Success;
    }
}
