using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Taskverse.Api.Mappings;
using Taskverse.Api.Models;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Controllers;

/// <summary>
/// Exposes result-related endpoints through the main API gateway.
/// </summary>
[Produces("application/json")]
public class ResultsController : TaskverseBaseController
{
    private const string StudentRole = "Student";
    private const string SuperAdminRole = "SuperAdmin";
    private const string CollegeAdminRole = "CollegeAdmin";
    private const string TrainerRole = "Trainer";

    private readonly IReportsOrchestrator _reportsOrchestrator;
    private readonly ILogger<ResultsController> _logger;

    public ResultsController(
        IReportsOrchestrator reportsOrchestrator,
        ILogger<ResultsController> logger)
    {
        _reportsOrchestrator = reportsOrchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Returns published assessment results for the specified student.
    /// </summary>
    /// <param name="studentId">The student identifier.</param>
    /// <returns>The student's result list.</returns>
    [HttpGet("/api/results/students/{studentId:guid}")]
    [SwaggerResponse(200, "Available results for the specified student", typeof(List<StudentResultResponseModel>))]
    [SwaggerResponse(400, "Invalid student id")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(503, "Reports microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> GetStudentResults(Guid studentId)
    {
        var accessCheck = EnsureStudentResultsAccess(studentId);
        if (accessCheck is not null) return accessCheck;

        if (studentId == Guid.Empty)
        {
            return BadRequest(new { message = "Student id is required." });
        }

        try
        {
            var dtos = await _reportsOrchestrator.GetStudentResults(studentId);
            return Ok(dtos.Select(dto => dto.ToResponseModel()).ToList());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var detail = ex.GetBaseException().Message;
            _logger.LogError(
                ex,
                "Unhandled student results retrieval error for studentId={StudentId}",
                studentId);
            return Problem(
                detail: detail,
                title: detail,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Returns the published result for a specific student attempt.
    /// </summary>
    /// <param name="attemptId">The attempt identifier.</param>
    /// <returns>The student's result for the requested attempt.</returns>
    [HttpGet("/api/results/students/attempts/{attemptId:guid}")]
    [SwaggerResponse(200, "Result for the specified student attempt", typeof(StudentResultResponseModel))]
    [SwaggerResponse(400, "Invalid attempt id")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Result not found")]
    [SwaggerResponse(503, "Reports microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> GetStudentAttemptResult(Guid attemptId)
    {
        var accessCheck = EnsureStudentAttemptResultAccess();
        if (accessCheck is not null) return accessCheck;

        if (attemptId == Guid.Empty)
        {
            return BadRequest(new { message = "Attempt id is required." });
        }

        try
        {
            var dto = await _reportsOrchestrator.GetStudentAttemptResult(attemptId);
            return Ok(dto.ToResponseModel());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var detail = ex.GetBaseException().Message;
            _logger.LogError(
                ex,
                "Unhandled student attempt result retrieval error for attemptId={AttemptId}",
                attemptId);
            return Problem(
                detail: detail,
                title: detail,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private IActionResult? EnsureStudentResultsAccess(Guid studentId)
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Forbid();
        }

        if (User.IsInRole(SuperAdminRole) ||
            User.IsInRole(CollegeAdminRole) ||
            User.IsInRole(TrainerRole))
        {
            return null;
        }

        if (User.IsInRole(StudentRole))
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue && currentUserId.Value == studentId)
            {
                return null;
            }
        }

        return Forbid();
    }

    private IActionResult? EnsureStudentAttemptResultAccess()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return Forbid();
        }

        if (User.IsInRole(SuperAdminRole) ||
            User.IsInRole(CollegeAdminRole) ||
            User.IsInRole(TrainerRole) ||
            User.IsInRole(StudentRole))
        {
            return null;
        }

        return Forbid();
    }

    private Guid? GetCurrentUserId()
    {
        var candidate = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(candidate) && Guid.TryParse(candidate, out var userIdFromClaims))
        {
            return userIdFromClaims;
        }

        candidate = Request?.Headers["UserId"].ToString();
        return Guid.TryParse(candidate, out var userId) ? userId : null;
    }
}
