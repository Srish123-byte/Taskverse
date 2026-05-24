using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using Taskverse.Api.Mappings;
using Taskverse.Api.Models;
using Taskverse.Business.Interface;
using Taskverse.Data.DataAccess;

namespace Taskverse.Api.Controllers;

[Route("api/assessments")]
[Produces("application/json")]
public class AssessmentsController : TaskverseBaseController
{
    private const string CollegeAdminRole = "CollegeAdmin";
    private const string TrainerRole = "Trainer";

    private readonly IAssessmentOrchestrator _assessmentOrchestrator;
    private readonly IDbContextFactory<TaskverseContext> _dbContextFactory;

    public AssessmentsController(
        IAssessmentOrchestrator assessmentOrchestrator,
        IDbContextFactory<TaskverseContext> dbContextFactory)
    {
        _assessmentOrchestrator = assessmentOrchestrator;
        _dbContextFactory = dbContextFactory;
    }

    [HttpPost]
    [SwaggerResponse(201, "Assessment created successfully", typeof(QuestionBankAssessmentResponseModel))]
    [SwaggerResponse(400, "Invalid request or CollegeId is missing/invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "One or more questions were not found")]
    [SwaggerResponse(409, "Assessment could not be created due to a conflict")]
    [SwaggerResponse(422, "Selected questions exceed the allowed assessment limit")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> CreateAssessment([FromBody] CreateQuestionBankAssessmentRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        if (model is null)
        {
            return BadRequest(new { message = "Assessment request is required." });
        }

        try
        {
            var trainerBatchAccessCheck = await EnsureTrainerCanAssignRequestedBatches(collegeId, model.AssignedBatchIds);
            if (trainerBatchAccessCheck is not null) return trainerBatchAccessCheck;

            var dto = await _assessmentOrchestrator.CreateAssessment(
                model.ToDto(collegeId, GetCreatedByName()));

            return StatusCode(StatusCodes.Status201Created, dto.ToResponseModel());
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
        catch (InvalidDataException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, title: "An unexpected error occurred while creating the assessment.");
        }
    }

    [HttpPost("{id:guid}/publish")]
    [SwaggerResponse(200, "Assessment published successfully", typeof(QuestionBankAssessmentResponseModel))]
    [SwaggerResponse(400, "Invalid request or CollegeId is missing/invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Assessment not found")]
    [SwaggerResponse(409, "Assessment could not be published due to a conflict")]
    [SwaggerResponse(422, "Assessment questions exceed allowed limits")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> PublishAssessment(Guid id)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out _);
        if (tenantCheck is not null) return tenantCheck;

        try
        {
            var dto = await _assessmentOrchestrator.PublishAssessment(id);
            return Ok(dto.ToResponseModel());
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
        catch (InvalidDataException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, title: "An unexpected error occurred while publishing the assessment.");
        }
    }

    [HttpPost("questions")]
    [SwaggerResponse(201, "Questions created successfully", typeof(List<QuestionResponseModel>))]
    [SwaggerResponse(400, "Invalid request or CollegeId header is missing/invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(409, "Questions could not be saved due to a conflict")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> CreateQuestion([FromBody] List<CreateQuestionRequestModel> models)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        if (models is null || models.Count == 0)
        {
            return BadRequest(new { message = "At least one question is required." });
        }

        try
        {
            var dtos = await _assessmentOrchestrator.CreateQuestions(
                models.ToDtos(collegeId, GetCreatedByName()));

            return StatusCode(StatusCodes.Status201Created, dtos.Select(dto => dto.ToResponseModel()).ToList());
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, title: "An unexpected error occurred while creating the questions.");
        }
    }

    [HttpPost("questions/search")]
    [SwaggerResponse(200, "Paged question bank result", typeof(PagedQuestionBankResponseModel))]
    [SwaggerResponse(400, "Invalid request or CollegeId header is missing/invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> SearchQuestionBank([FromBody] QuestionBankSearchRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        if (model is null)
        {
            return BadRequest(new { message = "Question bank search request is required." });
        }

        try
        {
            var dto = await _assessmentOrchestrator.SearchQuestionBank(model.ToDto(collegeId));
            return Ok(dto.ToResponseModel());
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
            return Problem(detail: ex.Message, title: "An unexpected error occurred while searching the question bank.");
        }
    }

    [HttpPut("questions/{id:guid}")]
    [SwaggerResponse(200, "Question updated successfully", typeof(QuestionResponseModel))]
    [SwaggerResponse(400, "Invalid request or CollegeId header is missing/invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Question not found")]
    [SwaggerResponse(409, "Question could not be updated due to a conflict")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> UpdateQuestion(Guid id, [FromBody] CreateQuestionRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        try
        {
            var dto = await _assessmentOrchestrator.UpdateQuestion(
                id,
                model.ToDto(collegeId, GetCreatedByName()));

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
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, title: "An unexpected error occurred while updating the question.");
        }
    }

    [HttpDelete("questions")]
    [SwaggerResponse(200, "Questions deleted successfully", typeof(DeleteQuestionsResponseModel))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "One or more questions were not found")]
    [SwaggerResponse(409, "One or more questions cannot be deleted")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> DeleteQuestion([FromBody] DeleteQuestionsRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        if (model is null || model.QuestionIds.Count == 0)
        {
            return BadRequest(new { message = "At least one question id is required." });
        }

        try
        {
            var deletedQuestionIds = await _assessmentOrchestrator.DeleteQuestions(
                model.ToDto(GetCreatedByName()));

            return Ok(deletedQuestionIds.ToResponseModel());
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
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, title: "An unexpected error occurred while deleting the questions.");
        }
    }

    private IActionResult? EnsureCollegeAdminOrTrainerAccess()
    {
        if (User?.Identity?.IsAuthenticated != true ||
            (!User.IsInRole(CollegeAdminRole) && !User.IsInRole(TrainerRole)))
        {
            return Forbid();
        }

        return null;
    }

    private IActionResult? TryGetCollegeId(out Guid collegeId)
    {
        if (!Guid.TryParse(CollegeId, out collegeId))
        {
            return BadRequest(new { message = "CollegeId header is missing or invalid." });
        }

        return null;
    }

    private string GetCreatedByName()
    {
        var fullName = User.FindFirstValue(ClaimTypes.Name);
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        var firstName = User.FindFirstValue(ClaimTypes.GivenName);
        var lastName = User.FindFirstValue(ClaimTypes.Surname);
        var combinedName = string.Join(" ", new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value)));

        if (!string.IsNullOrWhiteSpace(combinedName))
        {
            return combinedName;
        }

        return User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "unknown-user";
    }

    private async Task<IActionResult?> EnsureTrainerCanAssignRequestedBatches(Guid collegeId, IEnumerable<Guid>? requestedBatchIds)
    {
        if (!User.IsInRole(TrainerRole))
        {
            return null;
        }

        var normalizedBatchIds = (requestedBatchIds ?? [])
            .Where(batchId => batchId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (normalizedBatchIds.Length == 0)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Trainer assessments must be assigned to at least one batch."
            });
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Trainer user context is missing or invalid."
            });
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var trainer = await context.Trainers
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == currentUserId.Value && item.CollegeId == collegeId);

        if (trainer is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Trainer profile was not found for this college."
            });
        }

        var allowedBatchIds = await context.TrainerBatches
            .AsNoTracking()
            .Where(item => item.TrainerId == trainer.TrainerId)
            .Select(item => item.BatchId)
            .ToListAsync();

        var disallowedBatchIds = normalizedBatchIds
            .Except(allowedBatchIds)
            .ToArray();

        if (disallowedBatchIds.Length > 0)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = $"Trainer can only create assessments for assigned batches. Unassigned batch ids: {string.Join(", ", disallowedBatchIds)}."
            });
        }

        return null;
    }

    private Guid? GetCurrentUserId()
    {
        var candidate = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(candidate, out var userId) ? userId : null;
    }
}
