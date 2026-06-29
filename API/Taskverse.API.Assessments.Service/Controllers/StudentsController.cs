using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.API.Assessments.Service.Orchestrators;

namespace Taskverse.API.Assessments.Service.Controllers;

/// <summary>
/// Hosts student assessment and attempt endpoints inside the assessments microservice.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StudentsController : ControllerBase
{
    private readonly IAssessmentOrchestrator _assessmentOrchestrator;

    public StudentsController(IAssessmentOrchestrator assessmentOrchestrator)
    {
        _assessmentOrchestrator = assessmentOrchestrator;
    }

    /// <summary>
    /// Returns assessments visible to the supplied student for the requested statuses.
    /// </summary>
    /// <param name="request">The student assessment request.</param>
    /// <param name="assessmentStatuses">The statuses to include.</param>
    /// <returns>The student assessment list.</returns>
    [HttpPost("assessments")]
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
            var result = await _assessmentOrchestrator.GetStudentAssessments(request, assessmentStatuses);
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
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A database error occurred while retrieving student assessments.",
                "StudentAssessmentDatabaseError");
        }
        catch (PostgresException ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A PostgreSQL error occurred while retrieving student assessments.",
                "StudentAssessmentPostgresError");
        }
        catch (Exception ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while retrieving student assessments.");
        }
    }

    /// <summary>
    /// Returns the detail for a student's assigned assessment.
    /// </summary>
    /// <param name="assessmentId">The assessment identifier.</param>
    /// <param name="studentUserId">The student user identifier.</param>
    /// <returns>The student assessment detail.</returns>
    [HttpGet("assessments/{assessmentId:guid}")]
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
            var result = await _assessmentOrchestrator.GetStudentAssessmentDetail(assessmentId, studentUserId);
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
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A database error occurred while retrieving the student assessment detail.",
                "StudentAssessmentDetailDatabaseError");
        }
        catch (PostgresException ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A PostgreSQL error occurred while retrieving the student assessment detail.",
                "StudentAssessmentDetailPostgresError");
        }
        catch (Exception ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while retrieving the student assessment detail.");
        }
    }

    /// <summary>
    /// Starts an assessment attempt for the supplied student.
    /// </summary>
    /// <param name="assessmentId">The assessment identifier.</param>
    /// <param name="studentUserId">The student user identifier.</param>
    /// <returns>The started attempt state.</returns>
    [HttpPost("assessments/{assessmentId:guid}/start")]
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
            var result = await _assessmentOrchestrator.StartStudentAssessment(assessmentId, studentUserId);
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
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A database error occurred while starting the student assessment attempt.",
                "StudentAssessmentStartDatabaseError");
        }
        catch (PostgresException ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A PostgreSQL error occurred while starting the student assessment attempt.",
                "StudentAssessmentStartPostgresError");
        }
        catch (Exception ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while starting the student assessment attempt.");
        }
    }

    /// <summary>
    /// Recovers an in-progress assessment attempt for the supplied student.
    /// </summary>
    /// <param name="attemptId">The attempt identifier.</param>
    /// <param name="studentUserId">The student user identifier.</param>
    /// <returns>The recoverable attempt state.</returns>
    [HttpGet("attempts/{attemptId:guid}")]
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
            var result = await _assessmentOrchestrator.GetStudentAttemptRecovery(attemptId, studentUserId);
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
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A database error occurred while recovering the student assessment attempt.",
                "StudentAttemptRecoveryDatabaseError");
        }
        catch (PostgresException ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A PostgreSQL error occurred while recovering the student assessment attempt.",
                "StudentAttemptRecoveryPostgresError");
        }
        catch (Exception ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while recovering the student assessment attempt.");
        }
    }

    /// <summary>
    /// Submits an assessment attempt for the supplied student.
    /// </summary>
    /// <param name="attemptId">The attempt identifier.</param>
    /// <param name="studentUserId">The student user identifier.</param>
    /// <returns>The submitted attempt summary.</returns>
    [HttpPost("attempts/{attemptId:guid}/submit")]
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
            var result = await _assessmentOrchestrator.SubmitStudentAttempt(attemptId, studentUserId);
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
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A database error occurred while submitting the student assessment attempt.",
                "StudentAttemptSubmitDatabaseError");
        }
        catch (PostgresException ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A PostgreSQL error occurred while submitting the student assessment attempt.",
                "StudentAttemptSubmitPostgresError");
        }
        catch (Exception ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while submitting the student assessment attempt.");
        }
    }

    /// <summary>
    /// Returns the assessment streak summary for the supplied student.
    /// </summary>
    /// <param name="studentUserId">The student user identifier.</param>
    /// <returns>The student's streak summary.</returns>
    [HttpGet("me/streak")]
    [ProducesResponseType(typeof(StudentStreakRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StudentStreakRecord>> GetStudentStreak([FromQuery] Guid studentUserId)
    {
        try
        {
            var result = await _assessmentOrchestrator.GetStudentStreak(studentUserId);
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
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A database error occurred while retrieving the student streak summary.",
                "StudentStreakDatabaseError");
        }
        catch (PostgresException ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A PostgreSQL error occurred while retrieving the student streak summary.",
                "StudentStreakPostgresError");
        }
        catch (Exception ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while retrieving the student streak summary.");
        }
    }

    /// <summary>
    /// Saves an answer for a question within a student's assessment attempt.
    /// </summary>
    /// <param name="attemptId">The attempt identifier.</param>
    /// <param name="questionId">The question identifier.</param>
    /// <param name="studentUserId">The student user identifier.</param>
    /// <param name="request">The answer payload to save.</param>
    /// <returns>The saved answer state.</returns>
    [HttpPut("attempts/{attemptId:guid}/{questionId:guid}/answers")]
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
            var result = await _assessmentOrchestrator.SaveStudentAttemptAnswer(attemptId, questionId, studentUserId, request);
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
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A database error occurred while saving the student assessment answer.",
                "StudentAttemptAnswerDatabaseError");
        }
        catch (PostgresException ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "A PostgreSQL error occurred while saving the student assessment answer.",
                "StudentAttemptAnswerPostgresError");
        }
        catch (Exception ex)
        {
            return _assessmentOrchestrator.BuildUnexpectedError(
                ex,
                "An unexpected error occurred while saving the student assessment answer.");
        }
    }
}
