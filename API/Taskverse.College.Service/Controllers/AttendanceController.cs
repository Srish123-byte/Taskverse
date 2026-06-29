using Microsoft.AspNetCore.Mvc;
using Taskverse.API.College.Service.Mappings;
using Taskverse.API.College.Service.DTOs;
using Taskverse.API.College.Service.Models;
using Taskverse.API.College.Service.Orchestrators;

namespace Taskverse.API.College.Service.Controllers;

[ApiController]
[Route("api/attendance")]
[Produces("application/json")]
public class AttendanceController : ControllerBase
{
    private readonly ICollegeOrchestrator _collegeOrchestrator;

    public AttendanceController(ICollegeOrchestrator collegeOrchestrator)
    {
        _collegeOrchestrator = collegeOrchestrator;
    }

    [HttpGet("batches")]
    [ProducesResponseType(typeof(List<AttendanceBatchGroupRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AttendanceBatchGroupRecord>>> GetAttendanceBatches(
        [FromQuery] Guid collegeId,
        [FromQuery] Guid requesterUserId)
    {
        var result = await _collegeOrchestrator.GetAttendanceBatchGroups(collegeId, requesterUserId);
        return Ok(result.Select(item => item.ToModel()).ToList());
    }

    [HttpPost("roster")]
    [ProducesResponseType(typeof(AttendanceRosterRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceRosterRecord>> GetAttendanceRoster([FromBody] AttendanceRosterRequest request)
    {
        try
        {
            var result = await _collegeOrchestrator.GetAttendanceRoster(request.ToDto());
            return Ok(result.ToModel());
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
    [ProducesResponseType(typeof(AttendanceRosterRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttendanceRosterRecord>> SubmitAttendance([FromBody] SubmitAttendanceRequest request)
    {
        try
        {
            var result = await _collegeOrchestrator.SubmitAttendance(request.ToDto());
            return Ok(result.ToModel());
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
    [ProducesResponseType(typeof(AttendanceHistoryRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceHistoryRecord>> GetAttendanceHistory(
        [FromQuery] Guid collegeId,
        [FromQuery] Guid requesterUserId,
        [FromQuery] Guid batchId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        try
        {
            var result = await _collegeOrchestrator.GetAttendanceHistory(new AttendanceHistoryRequestDto
            {
                CollegeId = collegeId,
                RequesterUserId = requesterUserId,
                BatchId = batchId,
                FromDate = fromDate,
                ToDate = toDate
            });
 
            return Ok(result.ToModel());
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
    [ProducesResponseType(typeof(AttendanceExportRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceExportRecord>> ExportAttendance(
        [FromQuery] Guid collegeId,
        [FromQuery] Guid requesterUserId,
        [FromQuery] Guid batchId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        try
        {
            var result = await _collegeOrchestrator.ExportAttendance(new AttendanceHistoryRequestDto
            {
                CollegeId = collegeId,
                RequesterUserId = requesterUserId,
                BatchId = batchId,
                FromDate = fromDate,
                ToDate = toDate
            });

            return Ok(result.ToModel());
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
}
