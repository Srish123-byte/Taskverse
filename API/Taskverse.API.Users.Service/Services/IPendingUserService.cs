using Taskverse.API.Users.Service.DTOs;
using Taskverse.API.Users.Service.Models;

namespace Taskverse.API.Users.Service.Services;

public interface IPendingUserService
{
    Task<List<PendingUserDto>> GetPendingUsers();
    Task<PagedPendingUsersDto> SearchUsers(UserSearchRequestModel request);
}
