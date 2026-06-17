using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Taskverse.Api.Mappings;
using Taskverse.Api.Models;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Controllers;

[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SessionHealthController : TaskverseBaseController
{
    private const string StudentRole = "Student";
    private readonly IProctorOrchestrator _proctorOrchestrator;
    private readonly ILogger<SessionHealthController> _logger;

    public SessionHealthController(
        IProctorOrchestrator proctorOrchestrator,
        ILogger<SessionHealthController> logger)
    {
        _proctorOrchestrator = proctorOrchestrator;
        _logger = logger;
    }

    [HttpPost("sessions/{sessionId:guid}/heartbeat")]
    [ProducesResponseType(typeof(SessionHeartbeatResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HeartbeatSession(
        Guid sessionId,
        [FromBody] SessionHeartbeatRequestModel model)
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
            return BadRequest(new { message = "Session heartbeat request is required." });
        }

        try
        {
            var dto = await _proctorOrchestrator.HeartbeatSession(sessionId, model.ToDto(), currentUserId.Value);
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
                "Unhandled session heartbeat error for sessionId={SessionId}, userId={UserId}",
                sessionId,
                currentUserId.Value);

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
}
