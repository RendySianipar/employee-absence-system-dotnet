using EmployeeAbsence.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EmployeeService.Api.Notifications;

/// <summary>Real-time channel for the HR admin app. Only admins can connect.</summary>
[Authorize(Roles = Roles.Admin)]
public sealed class NotificationsHub : Hub
{
    public const string Path = "/hubs/notifications";
    public const string ProfileUpdatedMethod = "profile_updated";
}
