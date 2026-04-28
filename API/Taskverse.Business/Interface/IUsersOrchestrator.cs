using Taskverse.Business.DTOs;

namespace Taskverse.Business.Interface;

public interface IUsersOrchestrator
{
    Task<UserDto?> GetUser(string userId);
    Task<PagedUserDto?> SearchUsers(string? email, string? role, bool? isActive, int pageNumber, int pageSize);
    Task<UserDto?> CreateUser(CreateUserDto dto);
    Task<UserDto?> UpdateUser(string userId, UpdateUserDto dto);
    Task DeleteUser(string userId);
    Task<List<string>?> GetUserRoles(string userId);
}
