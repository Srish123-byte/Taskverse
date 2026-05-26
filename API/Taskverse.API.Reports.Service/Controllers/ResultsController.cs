using Microsoft.AspNetCore.Mvc;
using Taskverse.API.Reports.Service.Managers;
using Taskverse.API.Reports.Service.Models;

namespace Taskverse.API.Reports.Service.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly IResultManager _resultManager;

    public ResultsController(IResultManager resultManager)
    {
        _resultManager = resultManager;
    }

    [HttpPost("evaluate/{attemptId:guid}")]
    [ProducesResponseType(typeof(AttemptResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttemptResultResponse>> EvaluateAttempt(
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _resultManager.EvaluateAttemptAsync(attemptId, cancellationToken);
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
    }

    [HttpGet("attempts/{attemptId:guid}")]
    [ProducesResponseType(typeof(AttemptResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttemptResultResponse>> GetAttemptResult(
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _resultManager.GetAttemptResultAsync(attemptId, cancellationToken);
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
    }

    [HttpGet("students/{studentId:guid}")]
    [ProducesResponseType(typeof(List<StudentResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<StudentResultResponse>>> GetStudentResults(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _resultManager.GetStudentResultsAsync(studentId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
