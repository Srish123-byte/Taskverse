using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Taskverse.Api.Filters;
using Taskverse.Api.Models;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Controllers;

[Route("api/[controller]")]
[Produces("application/json")]
[ServiceFilter(typeof(JwtTokenValidationFilter))]
public class ProctorController : TaskverseBaseController
{
    private readonly IProctorOrchestrator _proctorOrchestrator;

    public ProctorController(IProctorOrchestrator proctorOrchestrator)
    {
        _proctorOrchestrator = proctorOrchestrator ?? throw new ArgumentNullException(nameof(proctorOrchestrator));
    }

    /// <summary>Starts a new proctoring session for an exam.</summary>
    [HttpPost("sessions")]
    [SwaggerResponse(201, "Session started", typeof(ProctorSessionResponseModel))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> StartSession([FromBody] StartProctorSessionRequestModel model)
    {
        try
        {
            var dto = await _proctorOrchestrator.StartSession(model.ExamId, model.UserId);
            return Created($"api/proctor/sessions/{dto.SessionId}", MapSession(dto));
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Gets a proctoring session by ID.</summary>
    [HttpGet("sessions/{sessionId}")]
    [SwaggerResponse(200, "Session found", typeof(ProctorSessionResponseModel))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Session not found")]
    public async Task<IActionResult> GetSession(string sessionId)
    {
        try
        {
            var dto = await _proctorOrchestrator.GetSession(sessionId);
            return Ok(MapSession(dto));
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Records a proctoring event.</summary>
    [HttpPost("events")]
    [SwaggerResponse(204, "Event recorded")]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> RecordEvent([FromBody] ProctorEventRequestModel model)
    {
        try
        {
            await _proctorOrchestrator.RecordEvent(model.SessionId, model.EventType, model.Payload);
            return NoContent();
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Ends a proctoring session.</summary>
    [HttpPut("sessions/{sessionId}/end")]
    [SwaggerResponse(204, "Session ended")]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Session not found")]
    public async Task<IActionResult> EndSession(string sessionId)
    {
        try
        {
            await _proctorOrchestrator.EndSession(sessionId);
            return NoContent();
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Gets the proctoring summary for a session.</summary>
    [HttpGet("sessions/{sessionId}/summary")]
    [SwaggerResponse(200, "Session summary", typeof(ProctorSummaryResponseModel))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Session not found")]
    public async Task<IActionResult> GetSummary(string sessionId)
    {
        try
        {
            var dto = await _proctorOrchestrator.GetSummary(sessionId);
            return Ok(new ProctorSummaryResponseModel
            {
                SessionId = dto.SessionId,
                TotalFlags = dto.TotalFlags,
                HighSeverityFlags = dto.HighSeverityFlags,
                IsApproved = dto.IsApproved,
                ReviewedBy = dto.ReviewedBy
            });
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    private static ProctorSessionResponseModel MapSession(ProctorSessionDto dto) => new()
    {
        SessionId = dto.SessionId,
        ExamId = dto.ExamId,
        UserId = dto.UserId,
        Status = dto.Status,
        StartedAt = dto.StartedAt,
        EndedAt = dto.EndedAt,
        TotalFlags = dto.TotalFlags
    };
}
