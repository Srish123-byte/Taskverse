using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
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
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpGet("registration/colleges")]
    [AllowAnonymous]
    [SwaggerResponse(200, "Approved colleges for registration", typeof(List<RegistrationCollegeResponseModel>))]
    public async Task<IActionResult> GetApprovedRegistrationColleges()
    {
        try
        {
            var colleges = await _usersOrchestrator.GetApprovedRegistrationColleges();
            return Ok(colleges.Select(college => college.ToResponseModel()).ToList());
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpGet("registration/colleges/{collegeId}/classes")]
    [AllowAnonymous]
    [SwaggerResponse(200, "Classes for a college", typeof(List<RegistrationClassResponseModel>))]
    public async Task<IActionResult> GetRegistrationClasses(string collegeId)
    {
        try
        {
            var classes = await _usersOrchestrator.GetRegistrationClasses(collegeId);
            return Ok(classes.Select(item => item.ToResponseModel()).ToList());
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    [HttpGet("registration/classes/{classId}/batches")]
    [AllowAnonymous]
    [SwaggerResponse(200, "Batches for a class", typeof(List<RegistrationBatchResponseModel>))]
    public async Task<IActionResult> GetRegistrationBatches(string classId)
    {
        try
        {
            var batches = await _usersOrchestrator.GetRegistrationBatches(classId);
            return Ok(batches.Select(batch => batch.ToResponseModel()).ToList());
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

}
