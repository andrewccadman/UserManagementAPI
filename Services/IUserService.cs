using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public interface IUserService
{
    Task<IEnumerable<User>> GetAllUsersAsync(int? page = null, int? pageSize = null);
    Task<User?> GetUserByIdAsync(int id);
    Task<User> CreateUserAsync(User user);
    Task<User?> UpdateUserAsync(int id, User user);
    Task<bool> DeleteUserAsync(int id);
    Task<int> GetTotalUserCountAsync();
}