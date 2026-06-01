using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Taskverse.API.Assessments.Service.Managers;
using Taskverse.API.Assessments.Service.Mappings;
using Taskverse.API.Assessments.Service.Models;

namespace Taskverse.API.Assessments.Service.Controllers;

[ApiController]
[Route("api/assessments")]
[Produces("application/json")]
public class AssessmentController : ControllerBase
{
    private const int MaxInstructionWordCount = 1000;
    private readonly IAssessmentManager _assessmentManager;
    private readonly AssessmentSettings _assessmentSettings;
    private readonly ILogger<AssessmentController> _logger;

    public AssessmentController(
        IAssessmentManager assessmentManager,
        IOptions<AssessmentSettings> assessmentSettings,
        ILogger<AssessmentController> logger)
    {
        _assessmentManager = assessmentManager;
        _assessmentSettings = assessmentSettings.Value;
        _logger = logger;
    }

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
            var assessment = await _assessmentManager.GetAssessment(id, collegeId, requesterRole, requesterName);
            return Ok(assessment.ToRecord());
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
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while retrieving the assessment.");
        }
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

        var instructionValidationError = ValidateInstructionWordLimit(request.Instructions);
        if (instructionValidationError is not null)
        {
            return BadRequest(new { message = instructionValidationError });
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
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while creating the assessment.");
        }
    }

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

        var instructionValidationError = ValidateInstructionWordLimit(request.Instructions);
        if (instructionValidationError is not null)
        {
            return BadRequest(new { message = instructionValidationError });
        }

        try
        {
            var assessment = await _assessmentManager.UpdateAssessment(id, request);
            return Ok(assessment.ToRecord());
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
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while updating the assessment.");
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
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while deleting the assessment.");
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
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while publishing the assessment.");
        }
    }

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

        if (request.AssessmentId.HasValue)
        {
            return await PublishAssessment(request.AssessmentId.Value);
        }

        var instructionValidationError = ValidateInstructionWordLimit(request.Instructions);
        if (instructionValidationError is not null)
        {
            return BadRequest(new { message = instructionValidationError });
        }

        try
        {
            var createRequest = request.ToCreateAssessmentRequest();
            var assessment = await _assessmentManager.ScheduleAssessment(
                createRequest.ToEntity(_assessmentSettings, Taskverse.Data.Enums.AssessmentStatus.Scheduled),
                createRequest.QuestionIds);

            return Ok(assessment.ToRecord());
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
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while publishing the assessment.");
        }
    }

    [HttpPost("subjects-topics/catalog")]
    [ProducesResponseType(typeof(AssessmentSubjectTopicCatalogRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssessmentSubjectTopicCatalogRecord>> GetSubjectTopicCatalog(
        [FromBody] AssessmentAccessibleBatchesRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Assessment bootstrap request is required." });
        }

        try
        {
            var result = await _assessmentManager.GetSubjectTopicCatalog(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while retrieving the subject-topic catalog.");
        }
    }

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
            var result = await _assessmentManager.GetTrainerAssignedClassesAndBatches(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while retrieving trainer assignment options.");
        }
    }

    [HttpPost("search")]
    [ProducesResponseType(typeof(AssessmentManagementSearchResultRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AssessmentManagementSearchResultRecord>> SearchAssessments(
        [FromBody] AssessmentManagementSearchRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Assessment search request is required." });
        }

        try
        {
            var result = await _assessmentManager.SearchAssessments(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while retrieving assessments.");
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
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while retrieving the assessment question list.");
        }
    }

    [HttpPost("/api/student/assessments")]
    [ProducesResponseType(typeof(List<StudentAssessmentListItemRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<StudentAssessmentListItemRecord>>> GetStudentAssessments(
        [FromBody] StudentAssessmentListRequest request,
        [FromQuery(Name = "assessmentStatuses")] string[] assessmentStatuses)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Student assessment request is required." });
        }

        try
        {
            var result = await _assessmentManager.GetStudentAssessments(request.StudentUserId, assessmentStatuses);
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
        catch (DbUpdateException ex)
        {
            return BuildUnexpectedError(
                ex,
                "A database error occurred while retrieving student assessments.",
                "StudentAssessmentDatabaseError");
        }
        catch (PostgresException ex)
        {
            return BuildUnexpectedError(
                ex,
                "A PostgreSQL error occurred while retrieving student assessments.",
                "StudentAssessmentPostgresError");
        }
        catch (Exception ex)
        {
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while retrieving student assessments.");
        }
    }

    [HttpGet("/api/student/assessments/{assessmentId:guid}")]
    [ProducesResponseType(typeof(StudentAssessmentDetailRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StudentAssessmentDetailRecord>> GetStudentAssessmentDetail(
        Guid assessmentId,
        [FromQuery] Guid studentUserId)
    {
        try
        {
            var result = await _assessmentManager.GetStudentAssessmentDetail(assessmentId, studentUserId);
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
        catch (DbUpdateException ex)
        {
            return BuildUnexpectedError(
                ex,
                "A database error occurred while retrieving the student assessment detail.",
                "StudentAssessmentDetailDatabaseError");
        }
        catch (PostgresException ex)
        {
            return BuildUnexpectedError(
                ex,
                "A PostgreSQL error occurred while retrieving the student assessment detail.",
                "StudentAssessmentDetailPostgresError");
        }
        catch (Exception ex)
        {
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while retrieving the student assessment detail.");
        }
    }

    [HttpPost("/api/student/assessments/{assessmentId:guid}/start")]
    [ProducesResponseType(typeof(StudentAssessmentStartRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StudentAssessmentStartRecord>> StartStudentAssessment(
        Guid assessmentId,
        [FromQuery] Guid studentUserId)
    {
        try
        {
            var result = await _assessmentManager.StartStudentAssessment(assessmentId, studentUserId);
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
        catch (DbUpdateException ex)
        {
            return BuildUnexpectedError(
                ex,
                "A database error occurred while starting the student assessment attempt.",
                "StudentAssessmentStartDatabaseError");
        }
        catch (PostgresException ex)
        {
            return BuildUnexpectedError(
                ex,
                "A PostgreSQL error occurred while starting the student assessment attempt.",
                "StudentAssessmentStartPostgresError");
        }
        catch (Exception ex)
        {
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while starting the student assessment attempt.");
        }
    }

    [HttpGet("/api/student/attempts/{attemptId:guid}")]
    [ProducesResponseType(typeof(StudentAttemptRecoveryRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StudentAttemptRecoveryRecord>> GetStudentAttemptRecovery(
        Guid attemptId,
        [FromQuery] Guid studentUserId)
    {
        try
        {
            var result = await _assessmentManager.GetStudentAttemptRecovery(attemptId, studentUserId);
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
        catch (DbUpdateException ex)
        {
            return BuildUnexpectedError(
                ex,
                "A database error occurred while recovering the student assessment attempt.",
                "StudentAttemptRecoveryDatabaseError");
        }
        catch (PostgresException ex)
        {
            return BuildUnexpectedError(
                ex,
                "A PostgreSQL error occurred while recovering the student assessment attempt.",
                "StudentAttemptRecoveryPostgresError");
        }
        catch (Exception ex)
        {
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while recovering the student assessment attempt.");
        }
    }

    [HttpPost("/api/student/attempts/{attemptId:guid}/submit")]
    [ProducesResponseType(typeof(StudentAttemptSubmitRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StudentAttemptSubmitRecord>> SubmitStudentAttempt(
        Guid attemptId,
        [FromQuery] Guid studentUserId)
    {
        try
        {
            var result = await _assessmentManager.SubmitStudentAttempt(attemptId, studentUserId);
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
        catch (DbUpdateException ex)
        {
            return BuildUnexpectedError(
                ex,
                "A database error occurred while submitting the student assessment attempt.",
                "StudentAttemptSubmitDatabaseError");
        }
        catch (PostgresException ex)
        {
            return BuildUnexpectedError(
                ex,
                "A PostgreSQL error occurred while submitting the student assessment attempt.",
                "StudentAttemptSubmitPostgresError");
        }
        catch (Exception ex)
        {
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while submitting the student assessment attempt.");
        }
    }

    [HttpPut("/api/student/attempts/{attemptId:guid}/{questionId:guid}/answers")]
    [ProducesResponseType(typeof(StudentAttemptAnswerRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StudentAttemptAnswerRecord>> SaveStudentAttemptAnswer(
        Guid attemptId,
        Guid questionId,
        [FromQuery] Guid studentUserId,
        [FromBody] SaveStudentAttemptAnswerRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Attempt answer request is required." });
        }

        try
        {
            var result = await _assessmentManager.SaveStudentAttemptAnswer(attemptId, questionId, studentUserId, request);
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
        catch (DbUpdateException ex)
        {
            return BuildUnexpectedError(
                ex,
                "A database error occurred while saving the student assessment answer.",
                "StudentAttemptAnswerDatabaseError");
        }
        catch (PostgresException ex)
        {
            return BuildUnexpectedError(
                ex,
                "A PostgreSQL error occurred while saving the student assessment answer.",
                "StudentAttemptAnswerPostgresError");
        }
        catch (Exception ex)
        {
            return BuildUnexpectedError(
                ex,
                "An unexpected error occurred while saving the student assessment answer.");
        }
    }

    private ObjectResult BuildUnexpectedError(Exception ex, string message, string name = "AssessmentServiceError")
    {
        var detail = ex.GetBaseException().Message;
        _logger.LogError(ex, "{Message} Detail: {Detail}", message, detail);

        return StatusCode(StatusCodes.Status500InternalServerError, new
        {
            name,
            message = detail,
            detail
        });
    }

    private static string? ValidateInstructionWordLimit(string? instructions)
    {
        return CountWords(instructions) > MaxInstructionWordCount
            ? $"Instructions cannot exceed {MaxInstructionWordCount} words."
            : null;
    }

    private static int CountWords(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? 0
            : normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
