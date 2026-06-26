using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Taskverse.Api.Mappings;
using Taskverse.Api.Models;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Controllers;

[Route("api/super-admin")]
[Produces("application/json")]
public class SuperAdminController : TaskverseBaseController
{
    private const string SuperAdminRole = "SuperAdmin";

    private readonly ISuperAdminOrchestrator _superAdminOrchestrator;

    public SuperAdminController(ISuperAdminOrchestrator superAdminOrchestrator)
    {
        _superAdminOrchestrator = superAdminOrchestrator;
    }

    [HttpGet("dashboard")]
    [SwaggerResponse(200, "Super admin dashboard", typeof(SuperAdminDashboardResponseModel))]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> GetDashboard()
    {
        var roleCheck = EnsureSuperAdmin();
        if (roleCheck is not null) return roleCheck;

        var dto = await _superAdminOrchestrator.GetDashboard();
        return Ok(dto.ToResponseModel());
    }

    [HttpGet("colleges")]
    [SwaggerResponse(200, "All colleges", typeof(List<CollegeResponseModel>))]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> GetColleges()
    {
        var roleCheck = EnsureSuperAdmin();
        if (roleCheck is not null) return roleCheck;

        var dto = await _superAdminOrchestrator.GetColleges();
        return Ok(dto.Select(x => x.ToResponseModel()).ToList());
    }

    [HttpPost("colleges/search")]
    [SwaggerResponse(200, "Filtered colleges", typeof(List<CollegeSearchResponseModel>))]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> SearchColleges([FromBody] CollegeSearchRequestModel model)
    {
        var roleCheck = EnsureSuperAdmin();
        if (roleCheck is not null) return roleCheck;

        var dto = await _superAdminOrchestrator.SearchColleges(model.ToDto());
        return Ok(dto.Select(x => x.ToResponseModel()).ToList());
    }

    [HttpGet("colleges/pending")]
    [SwaggerResponse(200, "Pending colleges", typeof(List<CollegeResponseModel>))]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> GetPendingColleges()
    {
        var roleCheck = EnsureSuperAdmin();
        if (roleCheck is not null) return roleCheck;

        var dto = await _superAdminOrchestrator.GetPendingColleges();
        return Ok(dto.Select(x => x.ToResponseModel()).ToList());
    }

    [HttpGet("users/pending")]
    [SwaggerResponse(200, "Pending users", typeof(List<PendingUserResponseModel>))]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> GetPendingUsers()
    {
        var roleCheck = EnsureSuperAdmin();
        if (roleCheck is not null) return roleCheck;

        var dto = await _superAdminOrchestrator.GetPendingUsers();
        return Ok(dto.Select(x => x.ToResponseModel()).ToList());
    }

    [HttpPost("users/search")]
    [SwaggerResponse(200, "Paged users search result", typeof(PagedUserSearchResponseModel))]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> SearchUsers([FromBody] UserSearchRequestModel model)
    {
        var roleCheck = EnsureSuperAdmin();
        if (roleCheck is not null) return roleCheck;

        var dto = await _superAdminOrchestrator.SearchUsers(model.ToDto());
        return Ok(dto.ToResponseModel());
    }

    [HttpPost("users/{userId}/approve")]
    [SwaggerResponse(204, "User approved")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> ApproveUser(string userId, [FromBody] UserActionRequestModel model)
    {
        var roleCheck = EnsureSuperAdmin();
        if (roleCheck is not null) return roleCheck;

        try
        {
            await _superAdminOrchestrator.ApproveUser(userId, model.ToDto(GetPerformedBy(), GetPerformedByUserId()));
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("users/{userId}/reject")]
    [SwaggerResponse(204, "User rejected")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> RejectUser(string userId, [FromBody] UserActionRequestModel model)
    {
        var roleCheck = EnsureSuperAdmin();
        if (roleCheck is not null) return roleCheck;

        try
        {
            await _superAdminOrchestrator.RejectUser(userId, model.ToDto(GetPerformedBy(), GetPerformedByUserId()));
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("users/bulk-upload/students")]
    [SwaggerResponse(200, "Bulk student upload processed", typeof(BulkStudentUploadResultResponseModel))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> BulkUploadStudents([FromBody] BulkStudentUploadRequestModel model)
    {
        var roleCheck = EnsureSuperAdmin();
        if (roleCheck is not null) return roleCheck;

        var uploadedByUserId = GetRequiredPerformedByUserId();
        if (uploadedByUserId is null)
        {
            return BadRequest(new { message = "The current user id could not be resolved." });
        }

        try
        {
            var result = await _superAdminOrchestrator.BulkUploadStudents(
                model.ToDto(uploadedByUserId.Value, GetPerformedBy(), GetPerformedBy()));
            return Ok(result.ToResponseModel());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("colleges/{collegeId}/approve")]
    [SwaggerResponse(200, "College approved", typeof(CollegeResponseModel))]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> ApproveCollege(string collegeId, [FromBody] CollegeActionRequestModel model)
    {
        var roleCheck = EnsureSuperAdmin();
        if (roleCheck is not null) return roleCheck;

        var dto = await _superAdminOrchestrator.ApproveCollege(collegeId, model.ToDto(GetPerformedBy()));
        return Ok(dto.ToResponseModel());
    }

    [HttpPost("colleges/{collegeId}/reject")]
    [SwaggerResponse(200, "College rejected", typeof(CollegeResponseModel))]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> RejectCollege(string collegeId, [FromBody] CollegeActionRequestModel model)
    {
        var roleCheck = EnsureSuperAdmin();
        if (roleCheck is not null) return roleCheck;

        var dto = await _superAdminOrchestrator.RejectCollege(collegeId, model.ToDto(GetPerformedBy()));
        return Ok(dto.ToResponseModel());
    }

    [HttpPost("colleges/{collegeId}/deactivate")]
    [SwaggerResponse(200, "College deactivated", typeof(CollegeResponseModel))]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> DeactivateCollege(string collegeId, [FromBody] CollegeActionRequestModel model)
    {
        var roleCheck = EnsureSuperAdmin();
        if (roleCheck is not null) return roleCheck;

        var dto = await _superAdminOrchestrator.DeactivateCollege(collegeId, model.ToDto(GetPerformedBy()));
        return Ok(dto.ToResponseModel());
    }

    [HttpPost("colleges/{collegeId}/reactivate")]
    [SwaggerResponse(200, "College reactivated", typeof(CollegeResponseModel))]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> ReactivateCollege(string collegeId, [FromBody] CollegeActionRequestModel model)
    {
        var roleCheck = EnsureSuperAdmin();
        if (roleCheck is not null) return roleCheck;

        var dto = await _superAdminOrchestrator.ReactivateCollege(collegeId, model.ToDto(GetPerformedBy()));
        return Ok(dto.ToResponseModel());
    }

    private IActionResult? EnsureSuperAdmin()
    {
        if (User?.Identity?.IsAuthenticated != true || !User.IsInRole(SuperAdminRole))
        {
            return Forbid();
        }

        return null;
    }

    private string GetPerformedBy()
    {
        return User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "super-admin";
    }

    private Guid? GetPerformedByUserId()
    {
        var candidate = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(candidate, out var userId) ? userId : null;
    }

    private Guid? GetRequiredPerformedByUserId() => GetPerformedByUserId();
}
