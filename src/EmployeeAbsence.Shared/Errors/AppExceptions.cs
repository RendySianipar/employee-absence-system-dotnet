namespace EmployeeAbsence.Shared;

/// <summary>Base type for business errors that map to an HTTP status code.</summary>
public abstract class AppException(string message, int statusCode) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class NotFoundException(string message) : AppException(message, 404);

public sealed class UnauthorizedException(string message) : AppException(message, 401);

public sealed class ForbiddenException(string message) : AppException(message, 403);

public sealed class ConflictException(string message) : AppException(message, 409);

public sealed class BusinessValidationException(string message) : AppException(message, 400);
