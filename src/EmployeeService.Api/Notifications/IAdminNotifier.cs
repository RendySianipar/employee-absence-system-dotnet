using EmployeeAbsence.Shared.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace EmployeeService.Api.Notifications;

public interface IAdminNotifier
{
    Task NotifyProfileUpdatedAsync(EmployeeProfileUpdatedEvent payload, CancellationToken cancellationToken = default);
}

public sealed class SignalRAdminNotifier(IHubContext<NotificationsHub> hub) : IAdminNotifier
{
    public Task NotifyProfileUpdatedAsync(EmployeeProfileUpdatedEvent payload, CancellationToken cancellationToken = default) =>
        hub.Clients.All.SendAsync(NotificationsHub.ProfileUpdatedMethod, payload, cancellationToken);
}
