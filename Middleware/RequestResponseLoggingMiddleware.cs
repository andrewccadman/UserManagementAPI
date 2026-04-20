using System.Text;
using System.Text.Json;

namespace UserManagementAPI.Middleware;

public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var requestBodyText = await ReadRequestBodyAsync(request);

        var originalResponseBodyStream = context.Response.Body;
        await using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        var requestLog = new
        {
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString.Value,
            Headers = request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            Body = requestBodyText
        };

        _logger.LogInformation("Incoming request: {Request}", JsonSerializer.Serialize(requestLog, new JsonSerializerOptions { WriteIndented = false }));

        await _next(context);

        // Check if response has been started (e.g., by exception handling middleware)
        if (context.Response.HasStarted)
        {
            // Response was already started, check if our MemoryStream has content
            if (responseBodyStream.Length > 0)
            {
                // Content was written to our MemoryStream, copy it to the original stream
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalResponseBodyStream);
            }
            // Restore the original stream
            context.Response.Body = originalResponseBodyStream;

            // Log the response that was written
            var responseBodyText = responseBodyStream.Length > 0 ? await ReadResponseBodyAsync(responseBodyStream) : "";
            var responseLog = new
            {
                StatusCode = context.Response.StatusCode,
                Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                Body = responseBodyText
            };

            _logger.LogInformation("Outgoing response: {Response}", JsonSerializer.Serialize(responseLog, new JsonSerializerOptions { WriteIndented = false }));
        }
        else
        {
            // Normal case: response not started, capture and log it
            var responseBodyText = await ReadResponseBodyAsync(responseBodyStream);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(originalResponseBodyStream);

            var responseLog = new
            {
                StatusCode = context.Response.StatusCode,
                Headers = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                Body = responseBodyText
            };

            _logger.LogInformation("Outgoing response: {Response}", JsonSerializer.Serialize(responseLog, new JsonSerializerOptions { WriteIndented = false }));
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength == null || request.ContentLength == 0)
        {
            return string.Empty;
        }

        request.EnableBuffering();
        request.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }

    private static async Task<string> ReadResponseBodyAsync(Stream responseBody)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBody, Encoding.UTF8, leaveOpen: true);
        var text = await reader.ReadToEndAsync();
        responseBody.Seek(0, SeekOrigin.Begin);
        return text;
    }
}

public static class RequestResponseLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
    }
}
