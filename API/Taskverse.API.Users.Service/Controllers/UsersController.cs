using Microsoft.AspNetCore.Mvc;
using Taskverse.API.Users.Service.Mappings;
using Taskverse.API.Users.Service.Models;
using Taskverse.API.Users.Service.Services;

namespace Taskverse.API.Users.Service.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IPendingUserService _pendingUserService;

    public UsersController(IPendingUserService pendingUserService)
    {
        _pendingUserService = pendingUserService;
    }

    [HttpGet("pending")]
    [ProducesResponseType(typeof(List<PendingUserResponseModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PendingUserResponseModel>>> GetPendingUsers()
    {
        var pendingUsers = await _pendingUserService.GetPendingUsers();
        return Ok(pendingUsers.Select(x => x.ToPendingUserResponseModel()).ToList());
    }
}
