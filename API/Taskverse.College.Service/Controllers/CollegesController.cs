using Microsoft.AspNetCore.Mvc;
using Taskverse.API.College.Service.Models;
using Taskverse.API.College.Service.Services;

namespace Taskverse.API.College.Service.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class CollegesController : ControllerBase
{
    private readonly ICollegeService _collegeService;

    public CollegesController(ICollegeService collegeService)
    {
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

    [HttpGet("colleges")]
    [ProducesResponseType(typeof(IReadOnlyList<CollegeRecord>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CollegeRecord>> GetColleges()
    {
        return Ok(_collegeService.GetColleges());
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
