using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.Mappings;
using Taskverse.Api.Models;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Controllers;

[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProctorController : TaskverseBaseController
{
    private const string StudentRole = "Student";
    private const string CollegeAdminRole = "CollegeAdmin";
    private const string TrainerRole = "Trainer";
    private readonly IProctorOrchestrator _proctorOrchestrator;
    private readonly ILogger<ProctorController> _logger;

    public ProctorController(
        IProctorOrchestrator proctorOrchestrator,
        ILogger<ProctorController> logger)
    {
        _proctorOrchestrator = proctorOrchestrator;
        _logger = logger;
    }

    [HttpPost("attempts/{attemptId:guid}/session")]
    [ProducesResponseType(typeof(ProctorSessionResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartProctoringSession(
        Guid attemptId,
        [FromBody] StartProctorSessionRequestModel model)
    {
        var accessCheck = EnsureStudentAccess();
        if (accessCheck is not null)
        {
            return accessCheck;
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Student user context is missing or invalid." });
        }

        if (model is null)
        {
            return BadRequest(new { message = "Start proctoring request is required." });
        }

        if (model.AttemptId != Guid.Empty && model.AttemptId != attemptId)
        {
            return BadRequest(new { message = "AttemptId in the request body must match the route attemptId." });
        }

        try
        {
            var dto = await _proctorOrchestrator.StartSession(model.ToDto(attemptId), currentUserId.Value);
            return Ok(dto.ToResponseModel());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var detail = ex.GetBaseException().Message;
            _logger.LogError(
                ex,
                "Unhandled proctoring session start error for attemptId={AttemptId}, userId={UserId}",
                attemptId,
                currentUserId.Value);

            return Problem(
                detail: detail,
                title: detail,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("session/{sessionId:guid}/event")]
    [ProducesResponseType(typeof(ProctorEventBatchResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RecordProctoringEvents(
        Guid sessionId,
        [FromBody] ProctorEventBatchRequestModel model)
    {
        var accessCheck = EnsureStudentAccess();
        if (accessCheck is not null)
        {
            return accessCheck;
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Student user context is missing or invalid." });
        }

        if (model is null)
        {
            return BadRequest(new { message = "Proctor event batch request is required." });
        }

        try
        {
            var dto = await _proctorOrchestrator.RecordEvents(sessionId, model.ToDto(), currentUserId.Value);
            return Ok(dto.ToResponseModel());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var detail = ex.GetBaseException().Message;
            _logger.LogError(
                ex,
                "Unhandled proctoring event batch error for sessionId={SessionId}, userId={UserId}",
                sessionId,
                currentUserId.Value);

            return Problem(
                detail: detail,
                title: detail,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("session/{sessionId:guid}/end")]
    [ProducesResponseType(typeof(ProctorSessionResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EndProctoringSession(
        Guid sessionId,
        [FromBody] EndProctorSessionRequestModel model)
    {
        var accessCheck = EnsureStudentAccess();
        if (accessCheck is not null)
        {
            return accessCheck;
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Student user context is missing or invalid." });
        }

        if (model is null)
        {
            return BadRequest(new { message = "End proctoring request is required." });
        }

        try
        {
            var dto = await _proctorOrchestrator.EndSession(sessionId, model.ToDto(), currentUserId.Value);
            return Ok(dto.ToResponseModel());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var detail = ex.GetBaseException().Message;
            _logger.LogError(
                ex,
                "Unhandled proctoring session end error for sessionId={SessionId}, userId={UserId}",
                sessionId,
                currentUserId.Value);

            return Problem(
                detail: detail,
                title: detail,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(ProctorSessionStateResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProctoringSessionState(Guid sessionId)
    {
        var accessCheck = EnsureStudentAccess();
        if (accessCheck is not null)
        {
            return accessCheck;
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Student user context is missing or invalid." });
        }

        try
        {
            var dto = await _proctorOrchestrator.GetSession(sessionId, currentUserId.Value);
            return Ok(dto.ToResponseModel());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var detail = ex.GetBaseException().Message;
            _logger.LogError(
                ex,
                "Unhandled proctoring session state retrieval error for sessionId={SessionId}, userId={UserId}",
                sessionId,
                currentUserId.Value);

            return Problem(
                detail: detail,
                title: detail,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("attempts/{attemptId:guid}")]
    [ProducesResponseType(typeof(ProctorSessionStateResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAttemptProctoringSession(Guid attemptId)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null)
        {
            return accessCheck;
        }

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null)
        {
            return tenantCheck;
        }

        var requesterName = GetCurrentUserName();
        var requesterRole = GetRequesterRole();

        try
        {
            var dto = await _proctorOrchestrator.GetAttemptSession(attemptId, collegeId, requesterRole, requesterName);
            return Ok(dto.ToResponseModel());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var detail = ex.GetBaseException().Message;
            _logger.LogError(
                ex,
                "Unhandled proctoring attempt session retrieval error for attemptId={AttemptId}, collegeId={CollegeId}, requesterRole={RequesterRole}",
                attemptId,
                collegeId,
                requesterRole);

            return Problem(
                detail: detail,
                title: detail,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private IActionResult? EnsureStudentAccess()
    {
        if (User?.Identity?.IsAuthenticated != true || !User.IsInRole(StudentRole))
        {
            return Forbid();
        }

        return null;
    }

    private Guid? GetCurrentUserId()
    {
        var candidate = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(candidate) && Guid.TryParse(candidate, out var userIdFromClaims))
        {
            return userIdFromClaims;
        }

        candidate = Request?.Headers["UserId"].ToString();
        if (!string.IsNullOrWhiteSpace(candidate) && Guid.TryParse(candidate, out var userIdFromHeader))
        {
            return userIdFromHeader;
        }

        return null;
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

    private string GetRequesterRole()
    {
        if (User.IsInRole(CollegeAdminRole))
        {
            return CollegeAdminRole;
        }

        if (User.IsInRole(TrainerRole))
        {
            return TrainerRole;
        }

        return UserRole;
    }

    private string GetCurrentUserName()
    {
        var fullName = User.FindFirstValue(ClaimTypes.Name);
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        var firstName = User.FindFirstValue(ClaimTypes.GivenName);
        var lastName = User.FindFirstValue(ClaimTypes.Surname);
        var combinedName = string.Join(" ", new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
        if (!string.IsNullOrWhiteSpace(combinedName))
        {
            return combinedName;
        }

        return User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "unknown-user";
    }
}
