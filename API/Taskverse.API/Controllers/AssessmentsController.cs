using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using Taskverse.Api.Mappings;
using Taskverse.Api.Models;
using Taskverse.Business.Interface;
using Taskverse.Data.DataAccess;

namespace Taskverse.Api.Controllers;

/// <summary>
/// Exposes assessment, question-bank, and student assessment endpoints through the main API gateway.
/// </summary>
[Route("api/assessments")]
[Produces("application/json")]
public class AssessmentsController : TaskverseBaseController
{
    private const int MaxInstructionWordCount = 1000;
    private const string SuperAdminRole = "SuperAdmin";
    private const string CollegeAdminRole = "CollegeAdmin";
    private const string TrainerRole = "Trainer";

    private readonly IAssessmentOrchestrator _assessmentOrchestrator;
    private readonly IReportsOrchestrator _reportsOrchestrator;
    private readonly IDbContextFactory<TaskverseContext> _dbContextFactory;
    private readonly ILogger<AssessmentsController> _logger;

    public AssessmentsController(
        IAssessmentOrchestrator assessmentOrchestrator,
        IReportsOrchestrator reportsOrchestrator,
        IDbContextFactory<TaskverseContext> dbContextFactory,
        ILogger<AssessmentsController> logger)
    {
        _assessmentOrchestrator = assessmentOrchestrator;
        _reportsOrchestrator = reportsOrchestrator;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    /// <summary>
    /// Creates an assessment for the current college-admin or trainer context.
    /// </summary>
    /// <param name="model">The assessment payload to create.</param>
    /// <returns>The created assessment response.</returns>
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

        var instructionValidationError = ValidateInstructionWordLimit(model.Instructions);
        if (instructionValidationError is not null)
        {
            return BadRequest(new { message = instructionValidationError });
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
            var detail = ex.GetBaseException().Message;
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Retrieves a single assessment that the current caller is allowed to view.
    /// </summary>
    /// <param name="id">The assessment identifier.</param>
    /// <returns>The requested assessment response.</returns>
    [HttpGet("{id:guid}")]
    [SwaggerResponse(200, "Assessment retrieved successfully", typeof(QuestionBankAssessmentResponseModel))]
    [SwaggerResponse(400, "Invalid request or CollegeId is missing/invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Assessment not found")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> GetAssessment(Guid id)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        try
        {
            var dto = await _assessmentOrchestrator.GetAssessment(
                id,
                collegeId,
                GetRequesterRole(),
                GetCreatedByName());

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
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var detail = ex.GetBaseException().Message;
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Updates an assessment for the current college-admin or trainer context.
    /// </summary>
    /// <param name="id">The assessment identifier.</param>
    /// <param name="model">The requested assessment updates.</param>
    /// <returns>The updated assessment response.</returns>
    [HttpPut("{id:guid}")]
    [SwaggerResponse(200, "Assessment updated successfully", typeof(QuestionBankAssessmentResponseModel))]
    [SwaggerResponse(400, "Invalid request or CollegeId is missing/invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Assessment not found")]
    [SwaggerResponse(409, "Assessment could not be updated due to a conflict")]
    [SwaggerResponse(422, "Selected questions exceed the allowed assessment limit")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> UpdateAssessment(Guid id, [FromBody] UpdateQuestionBankAssessmentRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        if (model is null)
        {
            return BadRequest(new { message = "Assessment update request is required." });
        }

        var instructionValidationError = ValidateInstructionWordLimit(model.Instructions);
        if (instructionValidationError is not null)
        {
            return BadRequest(new { message = instructionValidationError });
        }

        try
        {
            var trainerBatchAccessCheck = await EnsureTrainerCanAssignRequestedBatches(collegeId, model.AssignedBatchIds);
            if (trainerBatchAccessCheck is not null) return trainerBatchAccessCheck;

            var dto = await _assessmentOrchestrator.UpdateAssessment(
                model.ToDto(id, collegeId, GetCreatedByName(), GetRequesterRole()));

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
            var detail = ex.GetBaseException().Message;
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Publishes an existing assessment by identifier.
    /// </summary>
    /// <param name="id">The assessment identifier.</param>
    /// <returns>The published assessment response.</returns>
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
            var dto = await _assessmentOrchestrator.PublishAssessment(new Taskverse.Business.DTOs.PublishQuestionBankAssessmentDto
            {
                AssessmentId = id
            });
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
            var detail = ex.GetBaseException().Message;
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Soft deletes an assessment for an authorized requester.
    /// </summary>
    /// <param name="id">The assessment identifier.</param>
    /// <returns>A no-content response when deletion succeeds.</returns>
    [HttpDelete("{id:guid}")]
    [SwaggerResponse(204, "Assessment deleted successfully")]
    [SwaggerResponse(400, "Invalid request or CollegeId is missing/invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Assessment not found")]
    [SwaggerResponse(409, "Assessment could not be deleted due to a conflict")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> DeleteAssessment(Guid id)
    {
        var accessCheck = EnsureSuperAdminOrCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        Guid? collegeId = null;
        if (User.IsInRole(CollegeAdminRole) || User.IsInRole(TrainerRole))
        {
            var tenantCheck = TryGetCollegeId(out var parsedCollegeId);
            if (tenantCheck is not null) return tenantCheck;
            collegeId = parsedCollegeId;
        }

        try
        {
            await _assessmentOrchestrator.DeleteAssessment(new Taskverse.Business.DTOs.DeleteAssessmentDto
            {
                AssessmentId = id,
                IsDeleted = true,
                DeletedBy = GetCreatedByName(),
                RequesterRole = GetRequesterRole(),
                CollegeId = collegeId
            });

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
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var detail = ex.GetBaseException().Message;
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Creates one or more question-bank entries for the current college scope.
    /// </summary>
    /// <param name="models">The question payloads to create.</param>
    /// <returns>The created question responses.</returns>
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
            var detail = ex.GetBaseException().Message;
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Searches the question bank for the current college scope.
    /// </summary>
    /// <param name="model">The search filters and paging options.</param>
    /// <returns>The paged question-bank result.</returns>
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
            var detail = ex.GetBaseException().Message;
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Searches assessments for the management screens available to the current caller.
    /// </summary>
    /// <param name="model">The search filters and paging options.</param>
    /// <returns>The paged assessment management result.</returns>
    [HttpPost("search")]
    [SwaggerResponse(200, "Paged assessment management result", typeof(AssessmentManagementSearchResponseModel))]
    [SwaggerResponse(400, "Invalid request or CollegeId header is missing/invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> SearchAssessments([FromBody] AssessmentManagementSearchRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        if (model is null)
        {
            return BadRequest(new { message = "Assessment search request is required." });
        }

        try
        {
            var dto = await _assessmentOrchestrator.SearchAssessments(
                model.ToDto(collegeId, GetRequesterRole(), GetCurrentUserId(), GetCreatedByName()));

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
            var detail = ex.GetBaseException().Message;
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Returns the subject and topic catalog visible to the current caller.
    /// </summary>
    /// <returns>The accessible subject-topic catalog.</returns>
    [HttpGet("subjects-topics/catalog")]
    [SwaggerResponse(200, "Accessible subject-topic catalog for the assessment builder", typeof(AssessmentSubjectTopicCatalogResponseModel))]
    [SwaggerResponse(400, "Invalid request or CollegeId header is missing/invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> GetSubjectTopicCatalog()
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        try
        {
            var dto = new Taskverse.Business.DTOs.AssessmentBootstrapDto
            {
                CollegeId = collegeId,
                RequesterRole = GetRequesterRole(),
                RequesterUserId = GetCurrentUserId()
            };

            var result = await _assessmentOrchestrator.GetSubjectTopicCatalog(dto);
            return Ok(result.ToResponseModel());
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
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Publishes an existing assessment or creates and publishes a scheduled assessment.
    /// </summary>
    /// <param name="model">The publish request payload.</param>
    /// <returns>The published assessment response.</returns>
    [HttpPost("publish")]
    [SwaggerResponse(200, "Assessment published successfully", typeof(QuestionBankAssessmentResponseModel))]
    [SwaggerResponse(400, "Invalid request or CollegeId is missing/invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Assessment not found")]
    [SwaggerResponse(409, "Assessment could not be published due to a conflict")]
    [SwaggerResponse(422, "Assessment questions exceed allowed limits")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> PublishAssessment([FromBody] PublishQuestionBankAssessmentRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        if (model is null)
        {
            return BadRequest(new { message = "Assessment publish request is required." });
        }

        var instructionValidationError = ValidateInstructionWordLimit(model.Instructions);
        if (instructionValidationError is not null)
        {
            return BadRequest(new { message = instructionValidationError });
        }

        try
        {
            var requestedBatchIds = model.AssessmentId.HasValue ? [] : model.AssignedBatchIds;
            var trainerBatchAccessCheck = await EnsureTrainerCanAssignRequestedBatches(collegeId, requestedBatchIds);
            if (trainerBatchAccessCheck is not null) return trainerBatchAccessCheck;

            var dto = await _assessmentOrchestrator.PublishAssessment(
                model.ToDto(collegeId, GetCreatedByName()));

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
            var detail = ex.GetBaseException().Message;
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Returns the classes and batches assigned to the current trainer.
    /// </summary>
    /// <returns>The trainer assignment catalog.</returns>
    [HttpGet("trainer/assigned-classes-batches")]
    [SwaggerResponse(200, "Assigned classes and batches for the trainer assessment builder", typeof(AssessmentAssignmentCatalogResponseModel))]
    [SwaggerResponse(400, "Invalid request or CollegeId header is missing/invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> GetTrainerAssignedClassesAndBatches()
    {
        if (User?.Identity?.IsAuthenticated != true || !User.IsInRole(TrainerRole))
        {
            return Forbid();
        }

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Trainer user context is missing or invalid." });
        }

        try
        {
            var dto = new Taskverse.Business.DTOs.AssessmentBootstrapDto
            {
                CollegeId = collegeId,
                RequesterRole = TrainerRole,
                RequesterUserId = currentUserId.Value
            };

            var result = await _assessmentOrchestrator.GetTrainerAssignedClassesAndBatches(dto);
            return Ok(result.ToResponseModel());
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
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Retrieves a single question-bank entry by identifier.
    /// </summary>
    /// <param name="id">The question identifier.</param>
    /// <returns>The requested question response.</returns>
    [HttpGet("questions/{id:guid}")]
    [SwaggerResponse(200, "Question loaded successfully", typeof(QuestionResponseModel))]
    [SwaggerResponse(400, "Invalid request or CollegeId header is missing/invalid")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Question not found")]
    [SwaggerResponse(409, "Question cannot be edited while linked to a live assessment")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> GetQuestion(Guid id)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        try
        {
            var dto = await _assessmentOrchestrator.GetQuestion(id, collegeId);

            if (User.IsInRole(TrainerRole) &&
                !string.Equals(dto.CreatedBy?.Trim(), GetCreatedByName().Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    message = "You can only edit questions that you created."
                });
            }

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
            var detail = ex.GetBaseException().Message;
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Updates a question-bank entry by identifier.
    /// </summary>
    /// <param name="id">The question identifier.</param>
    /// <param name="model">The updated question payload.</param>
    /// <returns>The updated question response.</returns>
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
            model.RequesterRole = GetRequesterRole();
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
            var detail = ex.GetBaseException().Message;
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Deletes one or more question-bank entries for the current caller.
    /// </summary>
    /// <param name="model">The question identifiers and requester context.</param>
    /// <returns>The deleted question identifiers.</returns>
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

        var tenantCheck = TryGetCollegeId(out var collegeId);
        if (tenantCheck is not null) return tenantCheck;

        if (model is null || model.QuestionIds.Count == 0)
        {
            return BadRequest(new { message = "At least one question id is required." });
        }

        try
        {
            var deletedQuestionIds = await _assessmentOrchestrator.DeleteQuestions(
                model.ToDto(collegeId, GetCreatedByName(), GetRequesterRole()));

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
            var detail = ex.GetBaseException().Message;
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Returns a paged list of questions assigned to an assessment.
    /// </summary>
    /// <param name="id">The assessment identifier.</param>
    /// <param name="model">The paging request.</param>
    /// <returns>The paged assessment question list.</returns>
    [HttpPost("{id:guid}/questions/list")]
    [SwaggerResponse(200, "Paged question list for the assessment", typeof(PagedAssessmentQuestionListResponseModel))]
    [SwaggerResponse(400, "Invalid request or CollegeId header is missing/invalid")]
    [SwaggerResponse(403, "Forbidden — CollegeAdmin or Trainer role required")]
    [SwaggerResponse(404, "Assessment not found")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> GetAssessmentQuestionList(
        Guid id,
        [FromBody] AssessmentQuestionListRequestModel model)
    {
        var accessCheck = EnsureCollegeAdminOrTrainerAccess();
        if (accessCheck is not null) return accessCheck;

        var tenantCheck = TryGetCollegeId(out _);
        if (tenantCheck is not null) return tenantCheck;

        if (model is null)
        {
            return BadRequest(new { message = "Request body is required." });
        }

        var pageNumber = model.PageNumber > 0 ? model.PageNumber : 1;
        var pageSize   = model.PageSize is > 0 and <= 100 ? model.PageSize : 10;

        try
        {
            var dto = await _assessmentOrchestrator.GetAssessmentQuestionList(id, pageNumber, pageSize);
            return Ok(dto.ToResponseModel());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var detail = ex.GetBaseException().Message;
            return Problem(detail: detail, title: detail);
        }
    }

    /// <summary>
    /// Returns assessments available to the logged-in student for the requested statuses.
    /// </summary>
    /// <param name="assessmentStatuses">The statuses to include in the response.</param>
    /// <returns>The student assessment list.</returns>
    [HttpPost("/api/student/assessments")]
    [SwaggerResponse(200, "Assigned assessments for the logged-in student", typeof(List<StudentAssessmentListResponseModel>))]
    [SwaggerResponse(400, "Invalid assessment status filter or student context")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Student profile not found")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> GetStudentAssessments([FromQuery(Name = "assessmentStatuses")] string[] assessmentStatuses)
    {
        var accessCheck = EnsureStudentAccess();
        if (accessCheck is not null) return accessCheck;

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Student user context is missing or invalid." });
        }

        try
        {
            var dtos = await _assessmentOrchestrator.GetStudentAssessments(currentUserId.Value, assessmentStatuses);
            return Ok(dtos.Select(dto => dto.ToResponseModel()).ToList());
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
            _logger.LogError(ex, "Student assessments retrieval failed for userId={UserId}", currentUserId.Value);
            return Problem(
                detail: ex.Message,
                title: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var detail = ex.GetBaseException().Message;
            _logger.LogError(ex, "Unhandled student assessments retrieval error for userId={UserId}", currentUserId.Value);
            return Problem(
                detail: detail,
                title: detail,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Returns published assessment results for the specified student.
    /// </summary>
    /// <param name="studentId">The student identifier.</param>
    /// <returns>The student's result list.</returns>
    [HttpGet("/api/student/{studentId:guid}/results")]
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
    /// Returns the assessment detail required by the logged-in student to begin or continue an attempt.
    /// </summary>
    /// <param name="assessmentId">The assessment identifier.</param>
    /// <returns>The student assessment detail.</returns>
    [HttpGet("/api/student/assessments/{assessmentId:guid}")]
    [SwaggerResponse(200, "Assessment details for the logged-in student", typeof(StudentAssessmentDetailResponseModel))]
    [SwaggerResponse(400, "Invalid student context")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Assigned assessment not found")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> GetStudentAssessmentDetail(Guid assessmentId)
    {
        var accessCheck = EnsureStudentAccess();
        if (accessCheck is not null) return accessCheck;

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Student user context is missing or invalid." });
        }

        try
        {
            var dto = await _assessmentOrchestrator.GetStudentAssessmentDetail(assessmentId, currentUserId.Value);
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
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "Student assessment detail retrieval failed for assessmentId={AssessmentId}, userId={UserId}",
                assessmentId,
                currentUserId.Value);
            return Problem(
                detail: ex.Message,
                title: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
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
                "Unhandled student assessment detail retrieval error for assessmentId={AssessmentId}, userId={UserId}",
                assessmentId,
                currentUserId.Value);
            return Problem(
                detail: detail,
                title: detail,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Starts an assessment attempt for the logged-in student.
    /// </summary>
    /// <param name="assessmentId">The assessment identifier.</param>
    /// <returns>The started attempt response.</returns>
    [HttpPost("/api/student/assessments/{assessmentId:guid}/start")]
    [SwaggerResponse(200, "Assessment attempt started for the logged-in student", typeof(StudentAssessmentStartResponseModel))]
    [SwaggerResponse(400, "Invalid student context")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Assigned assessment not found")]
    [SwaggerResponse(409, "Assessment attempt cannot be started")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> StartStudentAssessment(Guid assessmentId)
    {
        var accessCheck = EnsureStudentAccess();
        if (accessCheck is not null) return accessCheck;

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Student user context is missing or invalid." });
        }

        try
        {
            var dto = await _assessmentOrchestrator.StartStudentAssessment(assessmentId, currentUserId.Value);
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
            var detail = ex.GetBaseException().Message;
            _logger.LogError(
                ex,
                "Unhandled student assessment start error for assessmentId={AssessmentId}, userId={UserId}",
                assessmentId,
                currentUserId.Value);
            return Problem(
                detail: detail,
                title: detail,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Recovers the in-progress attempt state for the logged-in student.
    /// </summary>
    /// <param name="attemptId">The attempt identifier.</param>
    /// <returns>The recoverable attempt state.</returns>
    [HttpGet("/api/student/attempts/{attemptId:guid}")]
    [SwaggerResponse(200, "Recoverable assessment attempt state for the logged-in student", typeof(StudentAttemptRecoveryResponseModel))]
    [SwaggerResponse(400, "Invalid student context")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Attempt not found")]
    [SwaggerResponse(409, "Attempt could not be recovered")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> GetStudentAttemptRecovery(Guid attemptId)
    {
        var accessCheck = EnsureStudentAccess();
        if (accessCheck is not null) return accessCheck;

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Student user context is missing or invalid." });
        }

        try
        {
            var dto = await _assessmentOrchestrator.GetStudentAttemptRecovery(attemptId, currentUserId.Value);
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
            var detail = ex.GetBaseException().Message;
            _logger.LogError(
                ex,
                "Unhandled student attempt recovery error for attemptId={AttemptId}, userId={UserId}",
                attemptId,
                currentUserId.Value);
            return Problem(
                detail: detail,
                title: detail,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Submits the specified assessment attempt for the logged-in student.
    /// </summary>
    /// <param name="attemptId">The attempt identifier.</param>
    /// <returns>The submitted attempt response.</returns>
    [HttpPost("/api/student/attempts/{attemptId:guid}/submit")]
    [SwaggerResponse(200, "Attempt submitted for the logged-in student", typeof(StudentAttemptSubmitResponseModel))]
    [SwaggerResponse(400, "Invalid student context or request")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Attempt not found")]
    [SwaggerResponse(409, "Attempt already submitted or auto-submitted")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> SubmitStudentAttempt(Guid attemptId)
    {
        var accessCheck = EnsureStudentAccess();
        if (accessCheck is not null) return accessCheck;

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Student user context is missing or invalid." });
        }

        try
        {
            var dto = await _assessmentOrchestrator.SubmitStudentAttempt(attemptId, currentUserId.Value);
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
            var detail = ex.GetBaseException().Message;
            _logger.LogError(
                ex,
                "Unhandled student attempt submit error for attemptId={AttemptId}, userId={UserId}",
                attemptId,
                currentUserId.Value);
            return Problem(
                detail: detail,
                title: detail,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Saves an answer for a single question within the logged-in student's attempt.
    /// </summary>
    /// <param name="attemptId">The attempt identifier.</param>
    /// <param name="questionId">The question identifier.</param>
    /// <param name="model">The answer payload to save.</param>
    /// <returns>The saved answer response.</returns>
    [HttpPut("/api/student/attempts/{attemptId:guid}/{questionId:guid}/answers")]
    [SwaggerResponse(200, "Attempt answer saved for the logged-in student", typeof(StudentAttemptAnswerResponseModel))]
    [SwaggerResponse(400, "Invalid student context or request")]
    [SwaggerResponse(403, "Forbidden")]
    [SwaggerResponse(404, "Attempt or question not found")]
    [SwaggerResponse(409, "Attempt is closed or expired")]
    [SwaggerResponse(503, "Assessments microservice is unavailable")]
    [SwaggerResponse(500, "Unexpected error")]
    public async Task<IActionResult> SaveStudentAttemptAnswer(
        Guid attemptId,
        Guid questionId,
        [FromBody] SaveStudentAttemptAnswerRequestModel model)
    {
        var accessCheck = EnsureStudentAccess();
        if (accessCheck is not null) return accessCheck;

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return BadRequest(new { message = "Student user context is missing or invalid." });
        }

        if (model is null)
        {
            return BadRequest(new { message = "Attempt answer request is required." });
        }

        try
        {
            var dto = await _assessmentOrchestrator.SaveStudentAttemptAnswer(
                attemptId,
                questionId,
                currentUserId.Value,
                model.ToDto());

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
            var detail = ex.GetBaseException().Message;
            _logger.LogError(
                ex,
                "Unhandled student attempt answer save error for attemptId={AttemptId}, userId={UserId}",
                attemptId,
                currentUserId.Value);
            return Problem(
                detail: detail,
                title: detail,
                statusCode: StatusCodes.Status500InternalServerError);
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

    private IActionResult? EnsureSuperAdminOrCollegeAdminAccess()
    {
        if (User?.Identity?.IsAuthenticated != true ||
            (!User.IsInRole(SuperAdminRole) && !User.IsInRole(CollegeAdminRole)))
        {
            return Forbid();
        }

        return null;
    }

    private IActionResult? EnsureStudentAccess()
    {
        if (User?.Identity?.IsAuthenticated != true || !User.IsInRole("Student"))
        {
            return Forbid();
        }

        return null;
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

        if (User.IsInRole("Student"))
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue && currentUserId.Value == studentId)
            {
                return null;
            }
        }

        return Forbid();
    }

    private IActionResult? EnsureSuperAdminOrCollegeAdminOrTrainerAccess()
    {
        if (User?.Identity?.IsAuthenticated != true ||
            (!User.IsInRole(SuperAdminRole) &&
             !User.IsInRole(CollegeAdminRole) &&
             !User.IsInRole(TrainerRole)))
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
        if (!string.IsNullOrWhiteSpace(candidate) && Guid.TryParse(candidate, out var userIdFromClaims))
        {
            return userIdFromClaims;
        }

        candidate = Request?.Headers["UserId"].ToString();
        return Guid.TryParse(candidate, out var userId) ? userId : null;
    }

    private string GetRequesterRole()
    {
        if (User.IsInRole(SuperAdminRole))
        {
            return SuperAdminRole;
        }

        if (User.IsInRole(CollegeAdminRole))
        {
            return CollegeAdminRole;
        }

        if (User.IsInRole(TrainerRole))
        {
            return TrainerRole;
        }

        return UserRole;
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
