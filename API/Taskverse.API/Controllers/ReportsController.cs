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
public class ReportsController : TaskverseBaseController
{
    private readonly IReportsOrchestrator _reportsOrchestrator;

    public ReportsController(IReportsOrchestrator reportsOrchestrator)
    {
        _reportsOrchestrator = reportsOrchestrator ?? throw new ArgumentNullException(nameof(reportsOrchestrator));
    }

    /// <summary>Generates a new report.</summary>
    [HttpPost("generate")]
    [SwaggerResponse(200, "Report generated", typeof(ReportResponseModel))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> GenerateReport([FromBody] GenerateReportRequestModel model)
    {
        try
        {
            var dto = await _reportsOrchestrator.GenerateReport(model.ToDto());
            return Ok(dto.ToResponseModel());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Gets a report by ID.</summary>
    [HttpGet("{reportId}")]
    [SwaggerResponse(200, "Report found", typeof(ReportResponseModel))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Report not found")]
    public async Task<IActionResult> GetReport(string reportId)
    {
        try
        {
            var dto = await _reportsOrchestrator.GetReport(reportId);
            return Ok(dto.ToResponseModel());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Gets the performance report for a user.</summary>
    [HttpGet("user/{userId}/performance")]
    [SwaggerResponse(200, "Performance report", typeof(UserPerformanceReportResponseModel))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> GetUserPerformanceReport(string userId)
    {
        try
        {
            var dto = await _reportsOrchestrator.GetUserPerformanceReport(userId);
            return Ok(dto.ToResponseModel());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Gets the report for an assessment.</summary>
    [HttpGet("assessment/{assessmentId}")]
    [SwaggerResponse(200, "Assessment report", typeof(AssessmentReportResponseModel))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> GetAssessmentReport(string assessmentId)
    {
        try
        {
            var dto = await _reportsOrchestrator.GetAssessmentReport(assessmentId);
            return Ok(dto.ToResponseModel());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Gets all reports for a user.</summary>
    [HttpGet("user/{userId}")]
    [SwaggerResponse(200, "User reports", typeof(List<ReportResponseModel>))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> GetReportsByUser(string userId)
    {
        try
        {
            var dtos = await _reportsOrchestrator.GetReportsByUser(userId);
            return Ok(dtos.Select(d => d.ToResponseModel()).ToList());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }
}
