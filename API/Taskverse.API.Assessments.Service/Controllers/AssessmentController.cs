using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Taskverse.API.Assessments.Service.Managers;
using Taskverse.API.Assessments.Service.Mappings;
using Taskverse.API.Assessments.Service.Models;

namespace Taskverse.API.Assessments.Service.Controllers;

[ApiController]
[Route("api/assessments")]
[Produces("application/json")]
public class AssessmentController : ControllerBase
{
    private readonly IAssessmentManager _assessmentManager;
    private readonly AssessmentSettings _assessmentSettings;

    public AssessmentController(
        IAssessmentManager assessmentManager,
        IOptions<AssessmentSettings> assessmentSettings)
    {
        _assessmentManager = assessmentManager;
        _assessmentSettings = assessmentSettings.Value;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssessmentRecord), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssessmentRecord>> CreateAssessment([FromBody] CreateAssessmentRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Assessment request is required." });
        }

        if (_assessmentSettings.AssessmentMaxDurationInMinutes > 0 &&
            request.DurationMinutes > _assessmentSettings.AssessmentMaxDurationInMinutes)
        {
            return BadRequest(new
            {
                message = $"Duration minutes cannot exceed {_assessmentSettings.AssessmentMaxDurationInMinutes}."
            });
        }

        try
        {
            var assessment = await _assessmentManager.CreateAssessment(
                request.ToEntity(_assessmentSettings),
                request.QuestionIds);

            var response = assessment.ToRecord();
            return Created($"api/assessments/{assessment.AssessmentId}", response);
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
        catch (AssessmentQuestionLimitException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while creating the assessment.",
                detail = ex.Message
            });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAssessment(Guid id, [FromBody] DeleteAssessmentRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Delete assessment request is required." });
        }

        if (request.AssessmentId != Guid.Empty && request.AssessmentId != id)
        {
            return BadRequest(new { message = "Assessment id in route and body must match." });
        }

        request.AssessmentId = id;

        try
        {
            await _assessmentManager.DeleteAssessment(id, request);
            return NoContent();
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
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while deleting the assessment.",
                detail = ex.Message
            });
        }
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(AssessmentRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssessmentRecord>> PublishAssessment(Guid id)
    {
        try
        {
            var assessment = await _assessmentManager.PublishAssessment(id);
            return Ok(assessment.ToRecord());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (AssessmentQuestionLimitException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while publishing the assessment.",
                detail = ex.Message
            });
        }
    }

    [HttpPost("{id:guid}/questions/list")]
    [ProducesResponseType(typeof(PagedAssessmentQuestionListRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedAssessmentQuestionListRecord>> GetAssessmentQuestionList(
        Guid id,
        [FromBody] AssessmentQuestionListRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        try
        {
            var result = await _assessmentManager.GetAssessmentQuestionList(
                id,
                request.PageNumber,
                request.PageSize);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while retrieving the assessment question list.",
                detail = ex.Message
            });
        }
    }
}
