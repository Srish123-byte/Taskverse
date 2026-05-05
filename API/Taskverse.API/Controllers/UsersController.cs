using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Taskverse.Api.Filters;
using Taskverse.Api.Mappings;
using Taskverse.Api.Models;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Controllers;

[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : TaskverseBaseController
{
    private readonly IUsersOrchestrator _usersOrchestrator;

    public UsersController(IUsersOrchestrator usersOrchestrator)
    {
        _usersOrchestrator = usersOrchestrator ?? throw new ArgumentNullException(nameof(usersOrchestrator));
    }

    /// <summary>
    /// Self-registration — publicly accessible, no JWT required.
    /// Creates a new user account. Non-SuperAdmin accounts are set to PENDING_APPROVAL.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [SwaggerResponse(201, "User registered successfully", typeof(UserResponseModel))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(409, "Email already registered")]
    public async Task<IActionResult> Register([FromBody] CreateUserRequestModel model)
    {
        try
        {
            var dto = await _usersOrchestrator.RegisterUser(model.ToDto());
            return Created($"api/users/{dto.UserId}", dto.ToResponseModel());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Gets a user by their ID.</summary>
    [HttpGet("{userId}")]
    [ServiceFilter(typeof(JwtTokenValidationFilter))]
    [SwaggerResponse(200, "User found", typeof(UserResponseModel))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> GetUser(string userId)
    {
        try
        {
            var dto = await _usersOrchestrator.GetUser(userId);
            return Ok(dto?.ToResponseModel());
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Searches users by criteria.</summary>
    [HttpPost("search")]
    [ServiceFilter(typeof(JwtTokenValidationFilter))]
    [SwaggerResponse(200, "Search results", typeof(PagedUserResponseModel))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> SearchUsers([FromBody] UserSearchRequestModel model)
    {
        try
        {
            var dto = await _usersOrchestrator.SearchUsers(model.Email, model.Role, model.IsActive, model.PageNumber, model.PageSize);
            return Ok(dto?.ToResponseModel());
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Creates a new user (admin use — requires JWT).</summary>
    [HttpPost]
    [ServiceFilter(typeof(JwtTokenValidationFilter))]
    [SwaggerResponse(201, "User created", typeof(UserResponseModel))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequestModel model)
    {
        try
        {
            var dto = await _usersOrchestrator.CreateUser(model.ToDto());
            return Created($"api/users/{dto?.UserId}", dto?.ToResponseModel());
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Updates an existing user.</summary>
    [HttpPut("{userId}")]
    [ServiceFilter(typeof(JwtTokenValidationFilter))]
    [SwaggerResponse(200, "User updated", typeof(UserResponseModel))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequestModel model)
    {
        try
        {
            var dto = await _usersOrchestrator.UpdateUser(userId, model.ToDto());
            return Ok(dto?.ToResponseModel());
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Deletes a user by ID.</summary>
    [HttpDelete("{userId}")]
    [ServiceFilter(typeof(JwtTokenValidationFilter))]
    [SwaggerResponse(204, "User deleted")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        try
        {
            await _usersOrchestrator.DeleteUser(userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Gets the roles assigned to a user.</summary>
    [HttpGet("{userId}/roles")]
    [ServiceFilter(typeof(JwtTokenValidationFilter))]
    [SwaggerResponse(200, "User roles", typeof(List<string>))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> GetUserRoles(string userId)
    {
        try
        {
            var roles = await _usersOrchestrator.GetUserRoles(userId);
            return Ok(roles);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }
}
