using Microsoft.AspNetCore.Mvc;
using Taskverse.API.Proctor.Service.Models;
using Taskverse.API.Proctor.Service.Orchestrators;

namespace Taskverse.API.Proctor.Service.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProctorController : ControllerBase
{
    private readonly IProctorOrchestrator _proctorOrchestrator;

    public ProctorController(IProctorOrchestrator proctorOrchestrator)
    {
        _proctorOrchestrator = proctorOrchestrator;
    }

    [HttpPost("attempts/{attemptId:guid}/session")]
    [ProducesResponseType(typeof(ProctorSessionRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProctorSessionRecord>> StartSession(
        Guid attemptId,
        [FromQuery] Guid studentUserId,
        [FromBody] StartProctorSessionRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Start proctoring request is required." });
        }

        try
        {
            var result = await _proctorOrchestrator.StartSession(attemptId, studentUserId, request);
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

    [HttpPost("session/{sessionId:guid}/event")]
    [ProducesResponseType(typeof(ProctorEventBatchResultRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProctorEventBatchResultRecord>> RecordEvents(
        Guid sessionId,
        [FromQuery] Guid studentUserId,
        [FromBody] ProctorEventBatchRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Proctor event batch request is required." });
        }

        try
        {
            var result = await _proctorOrchestrator.RecordEvents(sessionId, studentUserId, request);
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

    [HttpPost("session/{sessionId:guid}/end")]
    [ProducesResponseType(typeof(ProctorSessionRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProctorSessionRecord>> EndSession(
        Guid sessionId,
        [FromQuery] Guid studentUserId,
        [FromBody] EndProctorSessionRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "End proctoring request is required." });
        }

        try
        {
            var result = await _proctorOrchestrator.EndSession(sessionId, studentUserId, request);
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

    [HttpGet("sessions/{sessionId:guid}")]
    [ProducesResponseType(typeof(ProctorSessionStateRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProctorSessionStateRecord>> GetSessionState(
        Guid sessionId,
        [FromQuery] Guid studentUserId)
    {
        try
        {
            var result = await _proctorOrchestrator.GetSessionState(sessionId, studentUserId);
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
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.GetBaseException().Message });
        }
    }

    [HttpGet("attempts/{attemptId:guid}/session")]
    [ProducesResponseType(typeof(ProctorSessionStateRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProctorSessionStateRecord>> GetStudentSessionStateByAttempt(
        Guid attemptId,
        [FromQuery] Guid studentUserId)
    {
        try
        {
            var result = await _proctorOrchestrator.GetSessionStateByAttempt(attemptId, studentUserId);
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

    [HttpGet("attempts/{attemptId:guid}")]
    [ProducesResponseType(typeof(ProctorSessionStateRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProctorSessionStateRecord>> GetSessionStateByAttempt(Guid attemptId)
    {
        try
        {
            var result = await _proctorOrchestrator.GetSessionStateByAttempt(attemptId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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
