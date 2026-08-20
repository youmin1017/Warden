namespace Warden.Application.Common;

/// <summary>Base for exceptions that map to a specific HTTP status via middleware.</summary>
public abstract class AppException(string message) : Exception(message);

public sealed class NotFoundAppException(string message) : AppException(message);

public sealed class ConflictAppException(string message) : AppException(message);

public sealed class ValidationAppException(string message) : AppException(message);

public sealed class UnauthorizedAppException(string message) : AppException(message);
