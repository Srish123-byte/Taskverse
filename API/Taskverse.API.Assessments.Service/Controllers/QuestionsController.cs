using Microsoft.AspNetCore.Mvc;
using Taskverse.API.Assessments.Service.Managers;
using Taskverse.API.Assessments.Service.Mappings;
using Taskverse.API.Assessments.Service.Models;

namespace Taskverse.API.Assessments.Service.Controllers;

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
            var questions = await _questionManager.CreateQuestions(requests.Select(request => request.ToEntity()).ToList());
            var response = questions.Select(question => question.ToRecord()).ToList();

            return StatusCode(StatusCodes.Status201Created, response);
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
            var question = await _questionManager.UpdateQuestion(id, request.ToEntity());
            var response = question.ToRecord();

            return Ok(response);
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
            var deletedQuestionIds = await _questionManager.DeleteQuestions(request.CreatedBy, request.QuestionIds);
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
