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
}
