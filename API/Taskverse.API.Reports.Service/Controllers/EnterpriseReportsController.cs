using Microsoft.AspNetCore.Mvc;
using Taskverse.API.Reports.Service.Models;
using Taskverse.API.Reports.Service.Orchestrators;

namespace Taskverse.API.Reports.Service.Controllers;

[ApiController]
[Route("api/reports")]
public class EnterpriseReportsController : ControllerBase
{
    private readonly IEnterpriseReportsOrchestrator _orchestrator;

    public EnterpriseReportsController(IEnterpriseReportsOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpGet("filters/colleges")]
    public async Task<ActionResult<List<FilterOptionResponse>>> GetColleges(CancellationToken ct) =>
        Ok(await _orchestrator.GetCollegesAsync(ct));


    [HttpGet("filters/branches")]
    public async Task<ActionResult<List<FilterOptionResponse>>> GetBranches([FromQuery] Guid? collegeId, CancellationToken ct) =>
        Ok(await _orchestrator.GetBranchesAsync(collegeId, ct));

    [HttpGet("filters/batches")]
    public async Task<ActionResult<List<FilterOptionResponse>>> GetBatches([FromQuery] Guid? classId, CancellationToken ct) =>
        Ok(await _orchestrator.GetBatchesAsync(classId, ct));

    [HttpGet("filters/trainers")]
    public async Task<ActionResult<List<FilterOptionResponse>>> GetTrainers([FromQuery] Guid? collegeId, CancellationToken ct) =>
        Ok(await _orchestrator.GetTrainersAsync(collegeId, ct));

    [HttpGet("super-admin/college-wise")]
    public async Task<ActionResult<CollegeWiseReportResponse>> GetCollegeWiseReport(
        [FromQuery] Guid? collegeId, [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo, [FromQuery] string? academicYear,
        CancellationToken ct)
    {
        return Ok(await _orchestrator.GetCollegeWiseReportAsync(collegeId, dateFrom, dateTo, academicYear, ct));
    }

    [HttpGet("super-admin/college-wise/export/pdf")]
    public async Task<IActionResult> ExportCollegeWisePdf(
        [FromQuery] Guid? collegeId, [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo, [FromQuery] string? academicYear,
        CancellationToken ct)
    {
        var bytes = await _orchestrator.ExportCollegeWisePdfAsync(collegeId, dateFrom, dateTo, academicYear, ct);
        return File(bytes, "application/pdf", "college-wise-report.pdf");
    }

    [HttpGet("super-admin/college-wise/export/excel")]
    public async Task<IActionResult> ExportCollegeWiseExcel(
        [FromQuery] Guid? collegeId, [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo, [FromQuery] string? academicYear,
        CancellationToken ct)
    {
        var bytes = await _orchestrator.ExportCollegeWiseExcelAsync(collegeId, dateFrom, dateTo, academicYear, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "college-wise-report.xlsx");
    }

    [HttpGet("college-admin/branch-wise")]
    public async Task<ActionResult<BranchWiseReportResponse>> GetBranchWiseReport(
        [FromQuery] Guid? collegeId, [FromQuery] Guid? classId,
        [FromQuery] Guid? batchId, [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo, CancellationToken ct)
    {
        return Ok(await _orchestrator.GetBranchWiseReportAsync(collegeId, classId, batchId, dateFrom, dateTo, ct));
    }

    [HttpGet("college-admin/branch-wise/export/pdf")]
    public async Task<IActionResult> ExportBranchWisePdf(
        [FromQuery] Guid? collegeId, [FromQuery] Guid? classId,
        [FromQuery] Guid? batchId, [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo, CancellationToken ct)
    {
        var bytes = await _orchestrator.ExportBranchWisePdfAsync(collegeId, classId, batchId, dateFrom, dateTo, ct);
        return File(bytes, "application/pdf", "branch-wise-report.pdf");
    }

    [HttpGet("college-admin/branch-wise/export/excel")]
    public async Task<IActionResult> ExportBranchWiseExcel(
        [FromQuery] Guid? collegeId, [FromQuery] Guid? classId,
        [FromQuery] Guid? batchId, [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo, CancellationToken ct)
    {
        var bytes = await _orchestrator.ExportBranchWiseExcelAsync(collegeId, classId, batchId, dateFrom, dateTo, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "branch-wise-report.xlsx");
    }

    [HttpGet("trainer/student-performance")]
    public async Task<ActionResult<StudentPerformanceReportResponse>> GetStudentPerformanceReport(
        [FromQuery] Guid? collegeId, [FromQuery] Guid? classId,
        [FromQuery] Guid? batchId, [FromQuery] Guid? studentId,
        [FromQuery] Guid? trainerId, [FromQuery] Guid? assessmentId,
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo,
        [FromQuery] string? performanceLevel, CancellationToken ct)
    {
        return Ok(await _orchestrator.GetStudentPerformanceReportAsync(
            collegeId, classId, batchId, studentId, trainerId, assessmentId, dateFrom, dateTo, performanceLevel, ct));
    }

    [HttpGet("trainer/student-performance/export/pdf")]
    public async Task<IActionResult> ExportStudentPerformancePdf(
        [FromQuery] Guid? collegeId, [FromQuery] Guid? classId,
        [FromQuery] Guid? batchId, [FromQuery] Guid? studentId,
        [FromQuery] Guid? trainerId, [FromQuery] Guid? assessmentId,
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo,
        [FromQuery] string? performanceLevel, CancellationToken ct)
    {
        var bytes = await _orchestrator.ExportStudentPerformancePdfAsync(
            collegeId, classId, batchId, studentId, trainerId, assessmentId, dateFrom, dateTo, performanceLevel, ct);
        return File(bytes, "application/pdf", "student-performance-report.pdf");
    }

    [HttpGet("trainer/student-performance/export/excel")]
    public async Task<IActionResult> ExportStudentPerformanceExcel(
        [FromQuery] Guid? collegeId, [FromQuery] Guid? classId,
        [FromQuery] Guid? batchId, [FromQuery] Guid? studentId,
        [FromQuery] Guid? trainerId, [FromQuery] Guid? assessmentId,
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo,
        [FromQuery] string? performanceLevel, CancellationToken ct)
    {
        var bytes = await _orchestrator.ExportStudentPerformanceExcelAsync(
            collegeId, classId, batchId, studentId, trainerId, assessmentId, dateFrom, dateTo, performanceLevel, ct);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "student-performance-report.xlsx");
    }
}
