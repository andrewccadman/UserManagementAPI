using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UserManagementAPI.Middleware;
using UserManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// JWT configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings.GetValue<string>("SecretKey") ?? string.Empty;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    // Configure events to let exceptions bubble up to our middleware
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            // Convert authentication failure to an exception that our middleware can handle
            throw new System.UnauthorizedAccessException("Invalid or expired token");
        },
        OnChallenge = context =>
        {
            // Handle missing authorization header by throwing exception for consistent JSON response
            context.HandleResponse(); // Prevent default challenge response
            throw new System.UnauthorizedAccessException("Authorization header missing");
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.InvokeHandlersAfterFailure = false; // Don't invoke handlers after failure
    options.FallbackPolicy = null; // No fallback policy
});

// Add User Management Services
builder.Services.AddScoped<IUserService, UserService>();

// Add controllers
builder.Services.AddControllers();

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Add request/response logging first to capture all activity
app.UseRequestResponseLogging();

// Add exception handling to catch all errors
app.UseExceptionHandling();

app.UseAuthentication();
app.UseAuthorization();

// Convert status code responses such as 404 and 405 into JSON error payloads
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (response.StatusCode == 404 || response.StatusCode == 405)
    {
        if (!response.HasStarted)
        {
            response.ContentType = "application/json";
            var errorCode = response.StatusCode == 404 ? "NOT_FOUND" : "METHOD_NOT_ALLOWED";
            var message = response.StatusCode == 404 ? "Resource not found" : "Method not allowed";
            var payload = new
            {
                error = new
                {
                    code = errorCode,
                    message,
                    statusCode = response.StatusCode,
                    timestamp = DateTime.UtcNow
                }
            };
            await response.WriteAsync(JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

app.Run();
