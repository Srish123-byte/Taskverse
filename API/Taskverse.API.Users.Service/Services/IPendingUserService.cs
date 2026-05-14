using Taskverse.API.Users.Service.DTOs;

namespace Taskverse.API.Users.Service.Services;

public interface IPendingUserService
{
    Task<List<PendingUserDto>> GetPendingUsers();
}
