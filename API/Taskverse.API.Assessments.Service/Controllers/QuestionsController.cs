using Microsoft.AspNetCore.Mvc;
using Taskverse.API.Assessments.Service.Managers;
using Taskverse.API.Assessments.Service.Mappings;
using Taskverse.API.Assessments.Service.Models;

namespace Taskverse.API.Assessments.Service.Controllers;

/// <summary>
/// Hosts question-bank endpoints inside the assessments microservice.
/// </summary>
[ApiController]
[Route("api/questions")]
[Produces("application/json")]
public class QuestionsController : ControllerBase
{
    private readonly IQuestionManager _questionManager;

    public QuestionsController(IQuestionManager questionManager)
    {
        _questionManager = questionManager;
    }

    /// <summary>
    /// Returns the subject-topic catalog used by shared question creation flows.
    /// </summary>
    /// <returns>The available subjects and topics.</returns>
    [HttpGet("catalog")]
    [ProducesResponseType(typeof(QuestionClassificationCatalogRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuestionClassificationCatalogRecord>> GetQuestionClassificationCatalog()
    {
        try
        {
            var result = await _questionManager.GetQuestionClassificationCatalog();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while loading the question classification catalog.",
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Creates one or more question-bank entries in the microservice data store.
    /// </summary>
    /// <param name="requests">The question create requests.</param>
    /// <returns>The created question records.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(List<QuestionRecord>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<QuestionRecord>>> CreateQuestion([FromBody] List<CreateQuestionRequest> requests)
    {
        if (requests is null || requests.Count == 0)
        {
            return BadRequest(new { message = "At least one question is required." });
        }

        try
        {
            var questions = await _questionManager.CreateQuestions(
                requests.Select((request, index) => new QuestionImportItem
                {
                    InputOrder = index,
                    SourceRowNumber = request.SourceRowNumber ?? index + 2,
                    Request = request
                }).ToList());

            return StatusCode(StatusCodes.Status201Created, questions);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while creating the question.",
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Searches the question bank using the supplied filters and paging options.
    /// </summary>
    /// <param name="request">The question-bank search request.</param>
    /// <returns>The paged question-bank result.</returns>
    [HttpPost("search")]
    [ProducesResponseType(typeof(PagedQuestionRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedQuestionRecord>> SearchQuestionBank([FromBody] QuestionBankSearchRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { message = "Question bank search request is required." });
        }

        try
        {
            var result = await _questionManager.SearchQuestionBank(
                request.CollegeId,
                request.DifficultyLevel,
                request.SubjectId,
                request.TopicId,
                request.Subject,
                request.Topic,
                request.PageNumber,
                request.PageSize);

            return Ok(new PagedQuestionRecord(
                result.Items,
                result.TotalCount,
                request.PageNumber > 0 ? request.PageNumber : 1,
                request.PageSize is > 0 and <= 100 ? request.PageSize : 10));
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
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while searching the question bank.",
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Retrieves a question-bank entry by identifier.
    /// </summary>
    /// <param name="id">The question identifier.</param>
    /// <param name="collegeId">The college scope for the request.</param>
    /// <returns>The requested question record.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(QuestionRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuestionRecord>> GetQuestionById(
        Guid id,
        [FromQuery(Name = "collegeId")] Guid collegeId)
    {
        try
        {
            var question = await _questionManager.GetQuestionById(collegeId, id);
            return Ok(question);
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
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while loading the question.",
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Updates a question-bank entry by identifier.
    /// </summary>
    /// <param name="id">The question identifier.</param>
    /// <param name="request">The updated question payload.</param>
    /// <returns>The updated question record.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(QuestionRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<QuestionRecord>> UpdateQuestion(Guid id, [FromBody] CreateQuestionRequest request)
    {
        try
        {
            var question = await _questionManager.UpdateQuestion(id, request, request.RequesterRole);
            return Ok(question);
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
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while updating the question.",
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Deletes one or more question-bank entries after applying authorization and status checks.
    /// </summary>
    /// <param name="request">The delete request context.</param>
    /// <returns>The deleted question identifiers.</returns>
    [HttpDelete]
    [ProducesResponseType(typeof(List<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<Guid>>> DeleteQuestion([FromBody] DeleteQuestionsRequest request)
    {
        if (request is null || request.QuestionIds.Count == 0)
        {
            return BadRequest(new { message = "At least one question id is required." });
        }

        try
        {
            var deletedQuestionIds = await _questionManager.DeleteQuestions(
                request.CreatedBy,
                request.RequesterRole,
                request.CollegeId,
                request.QuestionIds);
            return Ok(deletedQuestionIds);
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
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "An unexpected error occurred while deleting the questions.",
                detail = ex.Message
            });
        }
    }
}
