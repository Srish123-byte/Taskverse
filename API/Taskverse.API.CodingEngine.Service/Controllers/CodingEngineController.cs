using Microsoft.AspNetCore.Mvc;
using Taskverse.API.CodingEngine.Service.Models;
using Taskverse.API.CodingEngine.Service.Orchestrators;

namespace Taskverse.API.CodingEngine.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CodingEngineController : ControllerBase
{
    private readonly ICodingEngineOrchestrator _codingEngineOrchestrator;

    public CodingEngineController(ICodingEngineOrchestrator codingEngineOrchestrator)
    {
        _codingEngineOrchestrator = codingEngineOrchestrator;
    }

    [HttpGet("assessments/{assessmentId:guid}/coding-questions/{codingQuestionId:guid}/editor-state")]
    [ProducesResponseType(typeof(EditorStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EditorStateResponse>> GetEditorState(
        Guid assessmentId,
        Guid codingQuestionId,
        [FromQuery] Guid studentUserId)
    {
        try
        {
            var result = await _codingEngineOrchestrator.GetEditorStateAsync(assessmentId, codingQuestionId, studentUserId);
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
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.GetBaseException().Message });
        }
    }

    [HttpPut("assessments/{assessmentId:guid}/coding-questions/{codingQuestionId:guid}/code")]
    [ProducesResponseType(typeof(SaveCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SaveCodeResponse>> SaveCode(
        Guid assessmentId,
        Guid codingQuestionId,
        [FromQuery] Guid studentUserId,
        [FromBody] SaveCodeRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Save code request is required." });
        }

        try
        {
            var result = await _codingEngineOrchestrator.SaveCodeAsync(assessmentId, codingQuestionId, studentUserId, request);
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
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.GetBaseException().Message });
        }
    }

    [HttpPut("assessments/{assessmentId:guid}/coding-questions/{codingQuestionId:guid}/run")]
    [ProducesResponseType(typeof(RunCodeQueuedResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RunCodeQueuedResponse>> RunCode(
        Guid assessmentId,
        Guid codingQuestionId,
        [FromQuery] Guid studentUserId,
        [FromBody] RunCodeRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Run code request is required." });
        }

        try
        {
            var result = await _codingEngineOrchestrator.RunCodeAsync(assessmentId, codingQuestionId, studentUserId, request);
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
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.GetBaseException().Message });
        }
    }

    [HttpGet("assessments/{assessmentId:guid}/executions/{executionRequestId:guid}")]
    [ProducesResponseType(typeof(RunCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RunCodeResponse>> GetExecutionStatus(
        Guid assessmentId,
        Guid executionRequestId,
        [FromQuery] Guid studentUserId)
    {
        try
        {
            var result = await _codingEngineOrchestrator.GetExecutionStatusAsync(assessmentId, executionRequestId, studentUserId);
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
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.GetBaseException().Message });
        }
    }
}
