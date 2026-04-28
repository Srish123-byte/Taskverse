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
public class ExamsController : TaskverseBaseController
{
    private readonly IExamOrchestrator _examOrchestrator;

    public ExamsController(IExamOrchestrator examOrchestrator)
    {
        _examOrchestrator = examOrchestrator ?? throw new ArgumentNullException(nameof(examOrchestrator));
    }

    /// <summary>Gets an exam by ID.</summary>
    [HttpGet("{examId}")]
    [SwaggerResponse(200, "Exam found", typeof(ExamResponseModel))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Exam not found")]
    public async Task<IActionResult> GetExam(string examId)
    {
        try
        {
            var dto = await _examOrchestrator.GetExam(examId);
            return Ok(dto?.ToResponseModel());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Creates a new exam.</summary>
    [HttpPost]
    [SwaggerResponse(201, "Exam created", typeof(ExamResponseModel))]
    [SwaggerResponse(400, "Invalid request")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> CreateExam([FromBody] CreateExamRequestModel model)
    {
        try
        {
            var dto = await _examOrchestrator.CreateExam(model.ToDto());
            return Created($"api/exams/{dto?.ExamId}", dto?.ToResponseModel());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Gets all questions for an exam.</summary>
    [HttpGet("{examId}/questions")]
    [SwaggerResponse(200, "Exam questions", typeof(List<QuestionResponseModel>))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Exam not found")]
    public async Task<IActionResult> GetExamQuestions(string examId)
    {
        try
        {
            var dtos = await _examOrchestrator.GetExamQuestions(examId);
            return Ok(dtos?.Select(q => q.ToResponseModel()).ToList());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Submits answers for an exam.</summary>
    [HttpPost("submit")]
    [SwaggerResponse(200, "Exam submitted", typeof(ExamResultResponseModel))]
    [SwaggerResponse(400, "Invalid submission")]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> SubmitExam([FromBody] ExamSubmissionRequestModel model)
    {
        try
        {
            var dto = await _examOrchestrator.SubmitExam(model.ToDto());
            return Ok(dto?.ToResponseModel());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Gets the result of an exam submission.</summary>
    [HttpGet("results/{submissionId}")]
    [SwaggerResponse(200, "Exam result", typeof(ExamResultResponseModel))]
    [SwaggerResponse(401, "Unauthorized")]
    [SwaggerResponse(404, "Result not found")]
    public async Task<IActionResult> GetExamResult(string submissionId)
    {
        try
        {
            var dto = await _examOrchestrator.GetExamResult(submissionId);
            return Ok(dto?.ToResponseModel());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }

    /// <summary>Gets all exams for a user.</summary>
    [HttpGet("user/{userId}")]
    [SwaggerResponse(200, "User exams", typeof(List<ExamResponseModel>))]
    [SwaggerResponse(401, "Unauthorized")]
    public async Task<IActionResult> GetExamsByUser(string userId)
    {
        try
        {
            var dtos = await _examOrchestrator.GetExamsByUser(userId);
            return Ok(dtos?.Select(e => e.ToResponseModel()).ToList());
        }
        catch (Exception ex) { return Problem(ex.Message); }
    }
}
