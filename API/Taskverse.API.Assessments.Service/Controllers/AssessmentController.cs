using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Taskverse.API.Assessments.Service.Managers;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.API.Assessments.Service.Orchestrators;

namespace Taskverse.API.Assessments.Service.Controllers;

/// <summary>
/// Hosts assessment endpoints inside the assessments microservice.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("api/assessments")]
[Produces("application/json")]
public class AssessmentController : ControllerBase
{
    private readonly IAssessmentOrchestrator _assessmentOrchestrator;

    public AssessmentController(IAssessmentOrchestrator assessmentOrchestrator)
    {
        _assessmentOrchestrator = assessmentOrchestrator;
    }

    /// <summary>
    /// Retrieves a single assessment after applying the supplied requester context.
    /// </summary>
    /// <param name="id">The assessment identifier.</param>
    /// <param name="collegeId">The college scope for the request.</param>
    /// <param name="requesterRole">The role of the caller.</param>
    /// <param name="requesterName">The display name of the caller.</param>
    /// <returns>The requested assessment record.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AssessmentRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssessmentRecord>> GetAssessment(
        Guid id,
        [FromQuery] Guid collegeId,
        [FromQuery] string requesterRole,
        [FromQuery] string requesterName)
    {
        try
        {
            var assessment = await _assessmentOrchestrator.GetAssessment(id, collegeId, requesterRole, requesterName);
            return Ok(assessment);
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
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while retrieving the assessment.");
        }
    }

    /// <summary>
    /// Creates a draft assessment in the microservice data store.
    /// </summary>
    /// <param name="request">The assessment create request.</param>
    /// <returns>The created assessment record.</returns>
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

        var instructionValidationError = _assessmentOrchestrator.ValidateInstructionWordLimit(request.Instructions);
        if (instructionValidationError is not null)
        {
            return BadRequest(new { message = instructionValidationError });
        }

        try
        {
            var assessment = await _assessmentOrchestrator.CreateAssessment(request);
            return Created($"{Request.Path}/{assessment.AssessmentId}", assessment);
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
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while creating the assessment.");
        }
    }

    /// <summary>
    /// Updates an existing assessment in the microservice data store.
    /// </summary>
    /// <param name="id">The assessment identifier.</param>
    /// <param name="request">The assessment update request.</param>
    /// <returns>The updated assessment record.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AssessmentRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssessmentRecord>> UpdateAssessment(Guid id, [FromBody] UpdateAssessmentRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Assessment update request is required." });
        }

        if (request.AssessmentId != Guid.Empty && request.AssessmentId != id)
        {
            return BadRequest(new { message = "Assessment id in route and body must match." });
        }

        request.AssessmentId = id;

        var instructionValidationError = _assessmentOrchestrator.ValidateInstructionWordLimit(request.Instructions);
        if (instructionValidationError is not null)
        {
            return BadRequest(new { message = instructionValidationError });
        }

        try
        {
            var assessment = await _assessmentOrchestrator.UpdateAssessment(id, request);
            return Ok(assessment);
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
        catch (AssessmentQuestionLimitException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while updating the assessment.");
        }
    }

    /// <summary>
    /// Soft deletes an existing assessment in the microservice data store.
    /// </summary>
    /// <param name="id">The assessment identifier.</param>
    /// <param name="request">The delete request context.</param>
    /// <returns>A no-content response when deletion succeeds.</returns>
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
            await _assessmentOrchestrator.DeleteAssessment(id, request);
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
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while deleting the assessment.");
        }
    }

    /// <summary>
    /// Publishes an existing assessment by identifier.
    /// </summary>
    /// <param name="id">The assessment identifier.</param>
    /// <returns>The published assessment record.</returns>
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
            var assessment = await _assessmentOrchestrator.PublishAssessment(id);
            return Ok(assessment);
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
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while publishing the assessment.");
        }
    }

    /// <summary>
    /// Publishes an existing assessment or creates and publishes a scheduled assessment.
    /// </summary>
    /// <param name="request">The publish request payload.</param>
    /// <returns>The published assessment record.</returns>
    [HttpPost("publish")]
    [ProducesResponseType(typeof(AssessmentRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssessmentRecord>> PublishAssessment([FromBody] PublishAssessmentRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Assessment publish request is required." });
        }

        var instructionValidationError = _assessmentOrchestrator.ValidateInstructionWordLimit(request.Instructions);
        if (instructionValidationError is not null)
        {
            return BadRequest(new { message = instructionValidationError });
        }

        try
        {
            var assessment = await _assessmentOrchestrator.PublishAssessment(request);
            return Ok(assessment);
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
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while publishing the assessment.");
        }
    }

    /// <summary>
    /// Returns the classes and batches assigned to a trainer.
    /// </summary>
    /// <param name="request">The trainer requester context.</param>
    /// <returns>The trainer assignment catalog.</returns>
    [HttpPost("trainer/assigned-classes-batches")]
    [ProducesResponseType(typeof(AssessmentAssignmentCatalogRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssessmentAssignmentCatalogRecord>> GetTrainerAssignedClassesAndBatches(
        [FromBody] AssessmentAccessibleBatchesRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Assessment bootstrap request is required." });
        }

        try
        {
            var result = await _assessmentOrchestrator.GetTrainerAssignedClassesAndBatches(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while retrieving trainer assignment options.");
        }
    }

    /// <summary>
    /// Searches assessments visible to the supplied requester context.
    /// </summary>
    /// <param name="request">The assessment search filters and requester context.</param>
    /// <returns>The paged assessment result with summary counts.</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedAssessmentSearchRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedAssessmentSearchRecord>> SearchAssessments([FromBody] AssessmentSearchRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Assessment search request is required." });
        }

        try
        {
            var result = await _assessmentOrchestrator.SearchAssessments(request);
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
        catch (Exception ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while searching assessments.");
        }
    }

    /// <summary>
    /// <summary>
    /// Returns a paged list of questions assigned to an assessment.
    /// </summary>
    /// <param name="id">The assessment identifier.</param>
    /// <param name="request">The paging request.</param>
    /// <returns>The paged assessment question list.</returns>
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
            var result = await _assessmentOrchestrator.GetAssessmentQuestionList(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while retrieving the assessment question list.");
        }
    }

}
