using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;
using UserManagementAPI.Services;
using UserManagementAPI.Exceptions;

namespace UserManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users with optional pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetAllUsers([FromQuery] int? page, [FromQuery] int? pageSize)
    {
        // Validate pagination parameters
        if (page.HasValue && page.Value <= 0)
        {
            throw new ValidationException("Invalid pagination parameters", new[] { "Page must be greater than 0" });
        }

        if (pageSize.HasValue && (pageSize.Value <= 0 || pageSize.Value > 100))
        {
            throw new ValidationException("Invalid pagination parameters", new[] { "Page size must be between 1 and 100" });
        }

        _logger.LogInformation("Getting users with pagination - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var users = await _userService.GetAllUsersAsync(page, pageSize);
        var totalCount = await _userService.GetTotalUserCountAsync();

        var result = new
        {
            Data = users,
            TotalCount = totalCount,
            Page = page ?? 1,
            PageSize = pageSize ?? totalCount,
            TotalPages = pageSize.HasValue ? (int)Math.Ceiling((double)totalCount / pageSize.Value) : 1
        };

        return Ok(result);
    }

    /// <summary>
    /// Get a user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserById(int id)
    {
        if (id <= 0)
        {
            throw new ValidationException("Invalid user ID", new[] { "User ID must be a positive integer" });
        }

        _logger.LogInformation("Getting user with ID: {UserId}", id);
        var user = await _userService.GetUserByIdAsync(id);

        if (user == null)
        {
            throw new ResourceNotFoundException($"User with ID {id} not found");
        }

        return Ok(user);
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<User>> CreateUser([FromBody] User user)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            _logger.LogWarning("Invalid user data provided for creation: {Errors}", string.Join(", ", errors));
            throw new ValidationException("Validation failed", errors);
        }

        // Additional custom validation
        if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName) || string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning("Required fields are missing for user creation");
            throw new ValidationException("Required fields are missing", new[] { "FirstName, LastName, and Email are required fields" });
        }

        _logger.LogInformation("Creating new user: {Email}", user.Email);
        var createdUser = await _userService.CreateUserAsync(user);

        _logger.LogInformation("User created successfully with ID: {UserId}", createdUser.Id);
        return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] User user)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            _logger.LogWarning("Invalid user data provided for update: {Errors}", string.Join(", ", errors));
            throw new ValidationException("Validation failed", errors);
        }

        // Additional custom validation
        if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName) || string.IsNullOrWhiteSpace(user.Email))
        {
            _logger.LogWarning("Required fields are missing for user update");
            throw new ValidationException("Required fields are missing", new[] { "FirstName, LastName, and Email are required fields" });
        }

        _logger.LogInformation("Updating user with ID: {UserId}", id);
        var updatedUser = await _userService.UpdateUserAsync(id, user);

        if (updatedUser == null)
        {
            throw new ResourceNotFoundException($"User with ID {id} not found");
        }

        _logger.LogInformation("User updated successfully with ID: {UserId}", id);
        return Ok(updatedUser);
    }

    /// <summary>
    /// Delete a user
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (id <= 0)
        {
            throw new ValidationException("Invalid user ID", new[] { "User ID must be a positive integer" });
        }

        _logger.LogInformation("Deleting user with ID: {UserId}", id);
        var result = await _userService.DeleteUserAsync(id);

        if (!result)
        {
            throw new ResourceNotFoundException($"User with ID {id} not found");
        }

        _logger.LogInformation("User deleted successfully with ID: {UserId}", id);
        return NoContent();
    }
}