using Microsoft.AspNetCore.Mvc;
using Taskverse.API.College.Service.Mappings;
using Taskverse.API.College.Service.Models;
using Taskverse.API.College.Service.Orchestrators;
using Taskverse.API.College.Service.Services;

namespace Taskverse.API.College.Service.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class CollegesController : ControllerBase
{
    private readonly ICollegeOrchestrator _collegeOrchestrator;
    private readonly ICollegeService _collegeService;

    public CollegesController(
        ICollegeOrchestrator collegeOrchestrator,
        ICollegeService collegeService)
    {
        _collegeOrchestrator = collegeOrchestrator;
        _collegeService = collegeService;
    }

    [HttpGet("registration/colleges")]
    [ProducesResponseType(typeof(List<RegistrationCollegeRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RegistrationCollegeRecord>>> GetApprovedRegistrationColleges()
    {
        var colleges = await _collegeService.GetApprovedRegistrationColleges();
        return Ok(colleges);
    }

    [HttpGet("registration/colleges/{collegeId:guid}/classes")]
    [ProducesResponseType(typeof(List<RegistrationClassRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RegistrationClassRecord>>> GetRegistrationClasses(Guid collegeId)
    {
        var classes = await _collegeService.GetRegistrationClasses(collegeId);
        return Ok(classes);
    }

    [HttpGet("registration/classes/{classId:guid}/batches")]
    [ProducesResponseType(typeof(List<RegistrationBatchRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RegistrationBatchRecord>>> GetRegistrationBatches(Guid classId)
    {
        var batches = await _collegeService.GetRegistrationBatches(classId);
        return Ok(batches);
    }

    [HttpGet("college-admins/{collegeAdminUserId:guid}/users/pending")]
    [ProducesResponseType(typeof(List<PendingUserRecord>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PendingUserRecord>>> GetPendingUsers(Guid collegeAdminUserId)
    {
        try
        {
            var dtos = await _collegeOrchestrator.GetPendingUsersForCollegeAdmin(collegeAdminUserId);
            return Ok(dtos.Select(dto => dto.ToModel()).ToList());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("colleges/{collegeId:guid}/users/pending")]
    [ProducesResponseType(typeof(List<PendingUserRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PendingUserRecord>>> GetPendingUsersByCollege(Guid collegeId)
    {
        var dtos = await _collegeOrchestrator.GetPendingUsersByCollege(collegeId);
        return Ok(dtos.Select(dto => dto.ToModel()).ToList());
    }

    [HttpGet("colleges/{collegeId:guid}/trainers/approved")]
    [ProducesResponseType(typeof(List<ApprovedTrainerRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ApprovedTrainerRecord>>> GetApprovedTrainers(Guid collegeId)
    {
        var dtos = await _collegeOrchestrator.GetApprovedTrainersByCollege(collegeId);
        return Ok(dtos.Select(dto => dto.ToModel()).ToList());
    }

    [HttpGet("subjects")]
    [ProducesResponseType(typeof(List<SubjectOptionRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SubjectOptionRecord>>> GetSubjects()
    {
        var dtos = await _collegeOrchestrator.GetSubjects();
        return Ok(dtos.Select(dto => dto.ToModel()).ToList());
    }

    [HttpGet("colleges")]
    [ProducesResponseType(typeof(IReadOnlyList<CollegeRecord>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CollegeRecord>> GetColleges()
    {
        return Ok(_collegeService.GetColleges());
    }

    [HttpPost("colleges/search")]
    [ProducesResponseType(typeof(List<CollegeSearchResultRecord>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CollegeSearchResultRecord>>> SearchColleges([FromBody] CollegeSearchRequest request)
    {
        var colleges = await _collegeService.SearchColleges(request);
        return Ok(colleges);
    }

    [HttpPost("colleges/{collegeId:guid}/classes")]
    [ProducesResponseType(typeof(CollegeClassSummaryRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CollegeClassSummaryRecord>> CreateClass(Guid collegeId, [FromBody] CreateCollegeClassRequest request)
    {
        try
        {
            var dto = await _collegeOrchestrator.CreateClass(collegeId, request.ToDto());
            return Ok(dto.ToModel());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("colleges/{collegeId:guid}/classes/{classId:guid}/batches")]
    [ProducesResponseType(typeof(CollegeBatchSummaryRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollegeBatchSummaryRecord>> CreateBatch(
        Guid collegeId,
        Guid classId,
        [FromBody] CreateCollegeBatchRequest request)
    {
        try
        {
            var dto = await _collegeOrchestrator.CreateBatch(collegeId, classId, request.ToDto());
            return Ok(dto.ToModel());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("colleges/{collegeId:guid}/classes/{classId:guid}/batches/{batchId:guid}/trainers")]
    [ProducesResponseType(typeof(CollegeBatchSummaryRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollegeBatchSummaryRecord>> AssignBatchTrainers(
        Guid collegeId,
        Guid classId,
        Guid batchId,
        [FromBody] AssignBatchTrainersRequest request)
    {
        try
        {
            var dto = await _collegeOrchestrator.AssignBatchTrainers(collegeId, classId, batchId, request.ToDto());
            return Ok(dto.ToModel());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("colleges/{collegeId:guid}/classes/{classId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteClass(Guid collegeId, Guid classId)
    {
        try
        {
            await _collegeOrchestrator.DeleteClass(collegeId, classId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("colleges/{collegeId:guid}/classes/{classId:guid}/batches/{batchId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBatch(Guid collegeId, Guid classId, Guid batchId)
    {
        try
        {
            await _collegeOrchestrator.DeleteBatch(collegeId, classId, batchId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("colleges/{collegeId:guid}/users/{userId}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveUser(
        Guid collegeId,
        string userId,
        [FromBody] CollegeUserActionRequest request)
    {
        try
        {
            await _collegeOrchestrator.ApproveUser(collegeId, userId, request.ToDto());
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("colleges/{collegeId:guid}/users/{userId}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectUser(
        Guid collegeId,
        string userId,
        [FromBody] CollegeUserActionRequest request)
    {
        try
        {
            await _collegeOrchestrator.RejectUser(collegeId, userId, request.ToDto());
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("colleges/pending")]
    [ProducesResponseType(typeof(List<CollegeRecord>), StatusCodes.Status200OK)]
    public ActionResult<List<CollegeRecord>> GetPendingColleges()
    {
        return Ok(_collegeService.GetPendingColleges());
    }

    [HttpGet("colleges/{id:guid}")]
    [ProducesResponseType(typeof(CollegeRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CollegeRecord> GetCollege(Guid id)
    {
        var college = _collegeService.GetCollege(id);
        return college is null ? NotFound() : Ok(college);
    }

    [HttpPost("colleges/{id:guid}/approve")]
    [ProducesResponseType(typeof(CollegeRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CollegeRecord> ApproveCollege(Guid id, [FromBody] CollegeActionRequest request)
    {
        var college = _collegeService.ApproveCollege(id, request);
        return college is null ? NotFound() : Ok(college);
    }

    [HttpPost("colleges/{id:guid}/reject")]
    [ProducesResponseType(typeof(CollegeRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CollegeRecord> RejectCollege(Guid id, [FromBody] CollegeActionRequest request)
    {
        var college = _collegeService.RejectCollege(id, request);
        return college is null ? NotFound() : Ok(college);
    }

    [HttpPost("colleges/{id:guid}/deactivate")]
    [ProducesResponseType(typeof(CollegeRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CollegeRecord> DeactivateCollege(Guid id, [FromBody] CollegeActionRequest request)
    {
        var college = _collegeService.DeactivateCollege(id, request);
        return college is null ? NotFound() : Ok(college);
    }

    [HttpPost("colleges/{id:guid}/reactivate")]
    [ProducesResponseType(typeof(CollegeRecord), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<CollegeRecord> ReactivateCollege(Guid id, [FromBody] CollegeActionRequest request)
    {
        var college = _collegeService.ReactivateCollege(id, request);
        return college is null ? NotFound() : Ok(college);
    }
}
