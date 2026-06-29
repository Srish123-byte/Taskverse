using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Taskverse.Api.Mappings;
using Taskverse.Api.Models;
using Taskverse.Business.DTOs;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Controllers;

[Route("api/attendance")]
[Produces("application/json")]
public class AttendanceController : TaskverseBaseController
{
    private const string CollegeAdminRole = "CollegeAdmin";
    private const string TrainerRole = "Trainer";

    private readonly IAttendanceOrchestrator _attendanceOrchestrator;

    public AttendanceController(IAttendanceOrchestrator attendanceOrchestrator)
    {
        _attendanceOrchestrator = attendanceOrchestrator;
    }

    [HttpGet("batches")]
    [SwaggerResponse(200, "Attendance batches grouped by class", typeof(List<AttendanceBatchGroupResponseModel>))]
    [SwaggerResponse(400, "CollegeId header is missing or invalid")]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> GetAttendanceBatches()
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Attendance user context is missing or invalid." });
        }

        var result = await _attendanceOrchestrator.GetAttendanceBatches(collegeId, currentUserId.Value);
        return Ok(result.Select(item => item.ToResponseModel()).ToList());
    }

    [HttpGet("roster")]
    [SwaggerResponse(200, "Attendance roster for the selected batch/date/session", typeof(AttendanceRosterResponseModel))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> GetAttendanceRoster(
        [FromQuery] Guid batchId,
        [FromQuery] DateTime attendanceDate,
        [FromQuery] Taskverse.Data.Enums.AttendanceSessionType attendanceSession)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Attendance user context is missing or invalid." });
        }

        try
        {
            var result = await _attendanceOrchestrator.GetAttendanceRoster(new AttendanceRosterRequestDto
            {
                CollegeId = collegeId,
                RequesterUserId = currentUserId.Value,
                BatchId = batchId,
                AttendanceDate = attendanceDate,
                AttendanceSession = attendanceSession
            });

            return Ok(result.ToResponseModel());
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

    [HttpPost("sessions")]
    [SwaggerResponse(200, "Attendance session submitted", typeof(AttendanceRosterResponseModel))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(409, "Attendance session conflict")]
    public async Task<IActionResult> SubmitAttendance([FromBody] SubmitAttendanceRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Attendance user context is missing or invalid." });
        }

        try
        {
            var result = await _attendanceOrchestrator.SubmitAttendance(model.ToDto(collegeId, currentUserId.Value));
            return Ok(result.ToResponseModel());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("history")]
    [SwaggerResponse(200, "Attendance history for the selected batch", typeof(AttendanceHistoryResponseModel))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> GetAttendanceHistory(
        [FromQuery] Guid batchId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Attendance user context is missing or invalid." });
        }

        try
        {
            var result = await _attendanceOrchestrator.GetAttendanceHistory(new AttendanceHistoryRequestDto
            {
                CollegeId = collegeId,
                RequesterUserId = currentUserId.Value,
                BatchId = batchId,
                FromDate = fromDate,
                ToDate = toDate
            });

            return Ok(result.ToResponseModel());
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

    [HttpGet("export")]
    [SwaggerResponse(200, "Attendance export file")]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> ExportAttendance(
        [FromQuery] Guid batchId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Attendance user context is missing or invalid." });
        }

        try
        {
            var result = await _attendanceOrchestrator.ExportAttendance(new AttendanceHistoryRequestDto
            {
                CollegeId = collegeId,
                RequesterUserId = currentUserId.Value,
                BatchId = batchId,
                FromDate = fromDate,
                ToDate = toDate
            });

            return File(Convert.FromBase64String(result.ContentBase64), result.ContentType, result.FileName);
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

    [HttpPost("email-report")]
    [SwaggerResponse(200, "Attendance report emailed")]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(403, "Forbidden")]
    public async Task<IActionResult> EmailAttendanceReport([FromBody] EmailAttendanceReportRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Attendance user context is missing or invalid." });
        }

        try
        {
            await _attendanceOrchestrator.EmailAttendanceReport(model.ToDto(collegeId, currentUserId.Value));
            return Ok(new { message = "Attendance report email sent successfully." });
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

    private IActionResult? EnsureCollegeAdminOrTrainerAccess()
    {
        if (User?.Identity?.IsAuthenticated != true ||
            (!User.IsInRole(CollegeAdminRole) && !User.IsInRole(TrainerRole)))
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

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? UserId;

        return Guid.TryParse(userIdClaim, out var parsedUserId)
            ? parsedUserId
            : null;
    }
}
