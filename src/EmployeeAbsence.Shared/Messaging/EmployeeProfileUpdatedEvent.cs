namespace EmployeeAbsence.Shared.Messaging;

/// <summary>
/// Published by Employee Service when an employee updates their own profile;
/// consumed by Logging Service. Sensitive values (password) are never included.
/// </summary>
public sealed record EmployeeProfileUpdatedEvent(
    Guid EmployeeId,
    string EmployeeName,
    string Action,
    IReadOnlyDictionary<string, string?> ChangedFields,
    DateTimeOffset Timestamp)
{
    public const string ProfileUpdatedAction = "PROFILE_UPDATED";
}
