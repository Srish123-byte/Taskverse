using Microsoft.AspNetCore.Mvc;
using Taskverse.API.Reports.Service.Models;
using Taskverse.API.Reports.Service.Orchestrators;

namespace Taskverse.API.Reports.Service.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController : ControllerBase
{
    private readonly IResultOrchestrator _resultOrchestrator;

    public ResultsController(IResultOrchestrator resultOrchestrator)
    {
        _resultOrchestrator = resultOrchestrator;
    }

    [HttpPost("evaluate")]
    [ProducesResponseType(typeof(AttemptResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AttemptResultResponse>> EvaluateAttempt(
        [FromBody] EvaluateAttemptRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Result evaluation request is required." });
        }

        try
        {
            var evaluation = await _resultOrchestrator.EvaluateAttemptAsync(
                request.AttemptId,
                request.PassingPercentage,
                cancellationToken);

            if (evaluation.WasSkipped)
            {
                return NoContent();
            }

            return Ok(evaluation.Result);
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
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    [HttpGet("students/{studentId:guid}")]
    [ProducesResponseType(typeof(List<StudentResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<StudentResultResponse>>> GetStudentResults(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _resultOrchestrator.GetStudentResultsAsync(studentId, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }
}
