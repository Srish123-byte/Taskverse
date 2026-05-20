using System.Security.Claims;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Taskverse.Api.Mappings;
using Taskverse.Api.Models;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Controllers;

[Route("api/college-admin")]
[Produces("application/json")]
public class CollegeAdminController : TaskverseBaseController
{
    private const string CollegeAdminRole = "CollegeAdmin";
    private static readonly ILog _log = LogManager.GetLogger(typeof(CollegeAdminController));

    private readonly ICollegeAdminOrchestrator _collegeAdminOrchestrator;

    public CollegeAdminController(ICollegeAdminOrchestrator collegeAdminOrchestrator)
    {
        _collegeAdminOrchestrator = collegeAdminOrchestrator;
    }

    [HttpGet("dashboard")]
    [SwaggerResponse(200, "College admin dashboard", typeof(CollegeAdminDashboardResponseModel))]
    [SwaggerResponse(400, "CollegeId header is missing or invalid")]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> GetDashboard()
    {
        var accessCheck = EnsureCollegeAdminAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        var dto = await _collegeAdminOrchestrator.GetDashboard(collegeId);
        return Ok(dto.ToResponseModel());
    }

    [HttpGet("classes")]
    [SwaggerResponse(200, "College classes and batches", typeof(ClassConfigurationResponseModel))]
    [SwaggerResponse(400, "CollegeId header is missing or invalid")]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> GetClasses()
    {
        var accessCheck = EnsureCollegeAdminAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        var dto = await _collegeAdminOrchestrator.GetClassConfiguration(collegeId);
        return Ok(dto.ToResponseModel());
    }

    [HttpGet("trainers/approved")]
    [SwaggerResponse(200, "Approved trainers for the college", typeof(List<ApprovedTrainerResponseModel>))]
    [SwaggerResponse(400, "CollegeId header is missing or invalid")]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> GetApprovedTrainers()
    {
        var accessCheck = EnsureCollegeAdminAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        var dtos = await _collegeAdminOrchestrator.GetApprovedTrainers(collegeId);
        return Ok(dtos.Select(dto => dto.ToResponseModel()).ToList());
    }

    [HttpPost("classes")]
    [SwaggerResponse(200, "Class created", typeof(CollegeClassSummaryResponseModel))]
    [SwaggerResponse(400, "CollegeId header is missing or invalid")]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> CreateClass([FromBody] CreateCollegeClassRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        try
        {
            var dto = await _collegeAdminOrchestrator.CreateClass(collegeId, model.ToDto());
            return Ok(dto.ToResponseModel());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("classes/{classId}/batches")]
    [SwaggerResponse(200, "Batch created", typeof(CollegeBatchSummaryResponseModel))]
    [SwaggerResponse(400, "CollegeId header is missing or invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Class not found")]
    public async Task<IActionResult> CreateBatch(string classId, [FromBody] CreateCollegeBatchRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        try
        {
            var dto = await _collegeAdminOrchestrator.CreateBatch(collegeId, classId, model.ToDto());
            return Ok(dto.ToResponseModel());
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

    [HttpPost("classes/{classId}/batches/{batchId}/trainers")]
    [SwaggerResponse(200, "Batch trainers assigned", typeof(CollegeBatchSummaryResponseModel))]
    [SwaggerResponse(400, "CollegeId header is missing or invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Class or batch not found")]
    public async Task<IActionResult> AssignBatchTrainers(
        string classId,
        string batchId,
        [FromBody] AssignBatchTrainersRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        try
        {
            var dto = await _collegeAdminOrchestrator.AssignBatchTrainers(collegeId, classId, batchId, model.ToDto());
            return Ok(dto.ToResponseModel());
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

    [HttpGet("users/pending")]
    [SwaggerResponse(200, "Pending users for the college", typeof(List<PendingUserResponseModel>))]
    [SwaggerResponse(400, "CollegeId header is missing or invalid")]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> GetPendingUsers()
    {
        var accessCheck = EnsureCollegeAdminAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        try
        {
            var dto = await _collegeAdminOrchestrator.GetPendingUsers(collegeId);
            return Ok(dto.Select(x => x.ToResponseModel()).ToList());
        }
        catch (InvalidOperationException ex)
        {
            _log.Warn($"CollegeAdminController.GetPendingUsers: invalid college mapping for collegeId={collegeId}", ex);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _log.Error($"CollegeAdminController.GetPendingUsers: unexpected error for collegeId={collegeId}", ex);
            return Problem("An unexpected error occurred while fetching pending users.");
        }
    }

    [HttpPost("users/{userId}/approve")]
    [SwaggerResponse(204, "User approved")]
    [SwaggerResponse(400, "CollegeId header is missing or invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> ApproveUser(string userId, [FromBody] UserActionRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        try
        {
            await _collegeAdminOrchestrator.ApproveUser(
                collegeId,
                userId,
                model.ToDto(GetPerformedBy(), GetPerformedByUserId()));
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
    [SwaggerResponse(400, "CollegeId header is missing or invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> RejectUser(string userId, [FromBody] UserActionRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        try
        {
            await _collegeAdminOrchestrator.RejectUser(
                collegeId,
                userId,
                model.ToDto(GetPerformedBy(), GetPerformedByUserId()));
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private IActionResult? EnsureCollegeAdminAccess()
    {
        if (User?.Identity?.IsAuthenticated != true || !User.IsInRole(CollegeAdminRole))
        {
            return Forbid();
        }

        return null;
    }

    private IActionResult? TryGetCollegeId(out Guid collegeId)
    {
        if (!Guid.TryParse(CollegeId, out collegeId))
        {
            return BadRequest(new { message = "CollegeId header is missing or invalid." });
        }

        return null;
    }

    private string GetPerformedBy()
    {
        return User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "college-admin";
    }

    private Guid? GetPerformedByUserId()
    {
        var candidate = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(candidate, out var userId) ? userId : null;
    }
}
