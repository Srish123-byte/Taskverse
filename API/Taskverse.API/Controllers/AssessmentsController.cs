using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Taskverse.Api.Filters;
using Taskverse.Api.Mappings;
using Taskverse.Api.Models;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Controllers;

[Route("api/[controller]")]
[Produces("application/json")]
[ServiceFilter(typeof(JwtTokenValidationFilter))]
public class AssessmentsController : TaskverseBaseController
{
    private readonly IAssessmentOrchestrator _assessmentOrchestrator;

    public AssessmentsController(IAssessmentOrchestrator assessmentOrchestrator)
    {
        _assessmentOrchestrator = assessmentOrchestrator ?? throw new ArgumentNullException(nameof(assessmentOrchestrator));
    }

    /// <summary>Gets an assessment by ID.</summary>
    [HttpGet("{assessmentId}")]
    [SwaggerResponse(200, "Assessment found", typeof(AssessmentResponseModel))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Assessment not found")]
    public async Task<IActionResult> GetAssessment(string assessmentId)
    {
        try
        {
            var dto = await _assessmentOrchestrator.GetAssessment(assessmentId);
            return Ok(dto.ToResponseModel());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Creates a new assessment.</summary>
    [HttpPost]
    [SwaggerResponse(201, "Assessment created", typeof(AssessmentResponseModel))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> CreateAssessment([FromBody] CreateAssessmentRequestModel model)
    {
        try
        {
            var dto = await _assessmentOrchestrator.CreateAssessment(model.ToDto());
            return Created($"api/assessments/{dto.AssessmentId}", dto.ToResponseModel());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Gets all assessments assigned to a user.</summary>
    [HttpGet("user/{userId}")]
    [SwaggerResponse(200, "Assessments found", typeof(List<AssessmentResponseModel>))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> GetAssessmentsByUser(string userId)
    {
        try
        {
            var dtos = await _assessmentOrchestrator.GetAssessmentsByUser(userId);
            return Ok(dtos.Select(d => d.ToResponseModel()).ToList());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Gets the result of an assessment for a specific user.</summary>
    [HttpGet("{assessmentId}/results/{userId}")]
    [SwaggerResponse(200, "Assessment result", typeof(AssessmentResultResponseModel))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Result not found")]
    public async Task<IActionResult> GetAssessmentResult(string assessmentId, string userId)
    {
        try
        {
            var dto = await _assessmentOrchestrator.GetAssessmentResult(assessmentId, userId);
            return Ok(dto.ToResponseModel());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Gets a summary of an assessment.</summary>
    [HttpGet("{assessmentId}/summary")]
    [SwaggerResponse(200, "Assessment summary", typeof(AssessmentSummaryResponseModel))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Assessment not found")]
    public async Task<IActionResult> GetAssessmentSummary(string assessmentId)
    {
        try
        {
            var dto = await _assessmentOrchestrator.GetAssessmentSummary(assessmentId);
            return Ok(dto.ToResponseModel());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }
}
