using Microsoft.AspNetCore.Mvc;
using Taskverse.API.Proctor.Service.Models;
using Taskverse.API.Proctor.Service.Orchestrators;

namespace Taskverse.API.Proctor.Service.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SessionHealthController : ControllerBase
{
    private readonly IProctorOrchestrator _proctorOrchestrator;

    public SessionHealthController(IProctorOrchestrator proctorOrchestrator)
    {
        _proctorOrchestrator = proctorOrchestrator;
    }

    [HttpPost("sessions/{sessionId:guid}/heartbeat")]
    [ProducesResponseType(typeof(SessionHeartbeatResponseRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SessionHeartbeatResponseRecord>> HeartbeatSession(
        Guid sessionId,
        [FromQuery] Guid studentUserId,
        [FromBody] SessionHeartbeatRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Session heartbeat request is required." });
        }

        try
        {
            var result = await _proctorOrchestrator.HeartbeatSession(sessionId, studentUserId, request);
            return Ok(result);
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
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.GetBaseException().Message });
        }
    }
}
