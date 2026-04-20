using UserManagementAPI.Models;
using System.Collections.Concurrent;

namespace UserManagementAPI.Services;

public class UserService : IUserService
{
    private static readonly ConcurrentDictionary<int, User> _users = new();
    private static int _nextId = 1;
    private static readonly object _idLock = new();

    public Task<IEnumerable<User>> GetAllUsersAsync(int? page = null, int? pageSize = null)
    {
        var users = _users.Values.ToList();

        // Apply pagination if specified
        if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
        {
            var skip = (page.Value - 1) * pageSize.Value;
            users = users.Skip(skip).Take(pageSize.Value).ToList();
        }

        return Task.FromResult(users.AsEnumerable());
    }

    public Task<int> GetTotalUserCountAsync()
    {
        return Task.FromResult(_users.Count);
    }

    public Task<User?> GetUserByIdAsync(int id)
    {
        // O(1) lookup instead of O(n) linear search
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User> CreateUserAsync(User user)
    {
        // Thread-safe ID generation
        int userId;
        lock (_idLock)
        {
            userId = _nextId++;
        }

        user.Id = userId;
        user.CreatedAt = DateTime.UtcNow;

        // Thread-safe addition to dictionary
        _users.TryAdd(userId, user);
        return Task.FromResult(user);
    }

    public Task<User?> UpdateUserAsync(int id, User user)
    {
        // O(1) lookup and update
        if (!_users.TryGetValue(id, out var existingUser))
            return Task.FromResult<User?>(null);

        // Create a new user object to avoid modifying the existing one directly
        // This ensures thread safety and immutability
        var updatedUser = new User
        {
            Id = existingUser.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = existingUser.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        // Atomic update using TryUpdate
        _users.TryUpdate(id, updatedUser, existingUser);
        return Task.FromResult<User?>(updatedUser);
    }

    public Task<bool> DeleteUserAsync(int id)
    {
        // O(1) removal
        return Task.FromResult(_users.TryRemove(id, out _));
    }
}