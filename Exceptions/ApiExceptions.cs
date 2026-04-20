namespace UserManagementAPI.Exceptions;

public class ApiException : Exception
{
    public int StatusCode { get; }
    public string ErrorCode { get; }

    public ApiException(string message, int statusCode, string errorCode) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}

public class UnauthorizedAccessException : ApiException
{
    public UnauthorizedAccessException(string message = "Unauthorized access")
        : base(message, 401, "UNAUTHORIZED") { }
}

public class ForbiddenAccessException : ApiException
{
    public ForbiddenAccessException(string message = "Access forbidden")
        : base(message, 403, "FORBIDDEN") { }
}

public class MethodNotAllowedException : ApiException
{
    public MethodNotAllowedException(string message = "Method not allowed")
        : base(message, 405, "METHOD_NOT_ALLOWED") { }
}

public class ResourceNotFoundException : ApiException
{
    public ResourceNotFoundException(string message = "Resource not found")
        : base(message, 404, "NOT_FOUND") { }
}

public class ValidationException : ApiException
{
    public IEnumerable<string> Errors { get; }

    public ValidationException(string message, IEnumerable<string> errors)
        : base(message, 400, "VALIDATION_ERROR")
    {
        Errors = errors;
    }
}