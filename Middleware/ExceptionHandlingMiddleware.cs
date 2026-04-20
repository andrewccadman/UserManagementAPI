using UserManagementAPI.Exceptions;
using System.Text.Json;

namespace UserManagementAPI.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApiException ex)
        {
            await HandleApiExceptionAsync(context, ex);
        }
        catch (System.UnauthorizedAccessException ex)
        {
            await HandleUnauthorizedAccessExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            await HandleUnexpectedExceptionAsync(context, ex);
        }
    }

    private async Task HandleApiExceptionAsync(HttpContext context, ApiException exception)
    {
        _logger.LogWarning(exception, "API Exception occurred: {Message}", exception.Message);

        try
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = exception.StatusCode;

            object response;

            if (exception is ValidationException validationEx)
            {
                response = new
                {
                    error = new
                    {
                        code = validationEx.ErrorCode,
                        message = validationEx.Message,
                        statusCode = exception.StatusCode,
                        timestamp = DateTime.UtcNow,
                        details = validationEx.Errors
                    }
                };
            }
            else
            {
                response = new
                {
                    error = new
                    {
                        code = exception.ErrorCode,
                        message = exception.Message,
                        statusCode = exception.StatusCode,
                        timestamp = DateTime.UtcNow
                    }
                };
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "Failed to write error response due to disposed stream. Original exception: {OriginalMessage}", exception.Message);
            // Stream is already disposed, cannot write response
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write error response. Original exception: {OriginalMessage}", exception.Message);
            // Fallback: try to set minimal response properties
            try
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = exception.StatusCode;
                    context.Response.ContentType = "application/json";
                }
            }
            catch
            {
                // Ignore any further errors
            }
        }
    }

    private async Task HandleUnauthorizedAccessExceptionAsync(HttpContext context, System.UnauthorizedAccessException exception)
    {
        _logger.LogWarning(exception, "Unauthorized access attempt: {Message}", exception.Message);

        try
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 401;

            var response = new
            {
                error = new
                {
                    code = "UNAUTHORIZED",
                    message = exception.Message,
                    statusCode = 401,
                    timestamp = DateTime.UtcNow
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "Failed to write unauthorized error response due to disposed stream. Original exception: {OriginalMessage}", exception.Message);
            // Stream is already disposed, cannot write response
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write unauthorized error response. Original exception: {OriginalMessage}", exception.Message);
            // Fallback: try to set minimal response properties
            try
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                }
            }
            catch
            {
                // Ignore any further errors
            }
        }
    }

    private async Task HandleUnexpectedExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unexpected error occurred: {Message}", exception.Message);

        try
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500;

            var response = new
            {
                error = new
                {
                    code = "INTERNAL_SERVER_ERROR",
                    message = "An internal server error occurred. Please try again later.",
                    timestamp = DateTime.UtcNow
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }
        catch (ObjectDisposedException ex)
        {
            _logger.LogError(ex, "Failed to write internal server error response due to disposed stream. Original exception: {OriginalMessage}", exception.Message);
            // Stream is already disposed, cannot write response
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write internal server error response. Original exception: {OriginalMessage}", exception.Message);
            // Fallback: try to set minimal response properties
            try
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";
                }
            }
            catch
            {
                // Ignore any further errors
            }
        }
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}