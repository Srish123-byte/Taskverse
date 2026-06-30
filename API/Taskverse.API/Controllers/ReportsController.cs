using Microsoft.AspNetCore.Mvc;
using Taskverse.Business.Interface;

namespace Taskverse.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportsOrchestrator _orchestrator;

    public ReportsController(IReportsOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  EXISTING ENDPOINTS (PRESERVED EXACTLY)
    // ═══════════════════════════════════════════════════════════════════════

    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportPdf(
        [FromQuery] string? collegeId,
        [FromQuery] string? classId,
        [FromQuery] string? batchId,
        [FromQuery] string? studentId,
        CancellationToken cancellationToken = default)
    {
        // Simply delegate to existing/new orchestrator mapping if desired, 
        // or support direct bytes from student performance pdf
        Guid.TryParse(collegeId, out var cid);
        Guid.TryParse(classId, out var clid);
        Guid.TryParse(batchId, out var bid);
        Guid.TryParse(studentId, out var sid);

        var bytes = await _orchestrator.ExportStudentPerformancePdf(
            cid == Guid.Empty ? null : cid,
            clid == Guid.Empty ? null : clid,
            bid == Guid.Empty ? null : bid,
            sid == Guid.Empty ? null : sid,
            null, null, null, null, null);

        return File(bytes, "application/pdf", "taskverse-results.pdf");
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel(
        [FromQuery] string? collegeId,
        [FromQuery] string? classId,
        [FromQuery] string? batchId,
        [FromQuery] string? studentId,
        CancellationToken cancellationToken = default)
    {
        Guid.TryParse(collegeId, out var cid);
        Guid.TryParse(classId, out var clid);
        Guid.TryParse(batchId, out var bid);
        Guid.TryParse(studentId, out var sid);

        var bytes = await _orchestrator.ExportStudentPerformanceExcel(
            cid == Guid.Empty ? null : cid,
            clid == Guid.Empty ? null : clid,
            bid == Guid.Empty ? null : bid,
            sid == Guid.Empty ? null : sid,
            null, null, null, null, null);

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "taskverse-results.xlsx");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FILTER OPTIONS ENDPOINTS
    // ═══════════════════════════════════════════════════════════════════════

    [HttpGet("filters/colleges")]
    public async Task<IActionResult> GetCollegeFilters() =>
        Ok(await _orchestrator.GetCollegesFilter());

    [HttpGet("filters/branches")]
    public async Task<IActionResult> GetBranchFilters([FromQuery] string? collegeId)
    {
        Guid.TryParse(collegeId, out var cid);
        return Ok(await _orchestrator.GetBranchesFilter(cid == Guid.Empty ? null : cid));
    }

    [HttpGet("filters/batches")]
    public async Task<IActionResult> GetBatchFilters([FromQuery] string? classId)
    {
        Guid.TryParse(classId, out var cid);
        return Ok(await _orchestrator.GetBatchesFilter(cid == Guid.Empty ? null : cid));
    }

    [HttpGet("filters/trainers")]
    public async Task<IActionResult> GetTrainerFilters([FromQuery] string? collegeId)
    {
        Guid.TryParse(collegeId, out var cid);
        return Ok(await _orchestrator.GetTrainersFilter(cid == Guid.Empty ? null : cid));
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SUPER ADMIN: COLLEGE-WISE REPORT
    // ═══════════════════════════════════════════════════════════════════════

    [HttpGet("super-admin/college-wise")]
    public async Task<IActionResult> GetCollegeWiseReport(
        [FromQuery] string? collegeId,
        [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo,
        [FromQuery] string? academicYear)
    {
        Guid.TryParse(collegeId, out var cid);
        DateTime.TryParse(dateFrom, out var dFrom);
        DateTime.TryParse(dateTo, out var dTo);

        var report = await _orchestrator.GetCollegeWiseReport(
            cid == Guid.Empty ? null : cid,
            dFrom == DateTime.MinValue ? null : dFrom,
            dTo == DateTime.MinValue ? null : dTo,
            academicYear);

        return Ok(report);
    }

    [HttpGet("super-admin/college-wise/export/pdf")]
    public async Task<IActionResult> ExportCollegeWisePdf(
        [FromQuery] string? collegeId, [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo, [FromQuery] string? academicYear)
    {
        Guid.TryParse(collegeId, out var cid);
        DateTime.TryParse(dateFrom, out var dFrom);
        DateTime.TryParse(dateTo, out var dTo);

        var bytes = await _orchestrator.ExportCollegeWisePdf(
            cid == Guid.Empty ? null : cid,
            dFrom == DateTime.MinValue ? null : dFrom,
            dTo == DateTime.MinValue ? null : dTo,
            academicYear);

        return File(bytes, "application/pdf", "college-wise-report.pdf");
    }

    [HttpGet("super-admin/college-wise/export/excel")]
    public async Task<IActionResult> ExportCollegeWiseExcel(
        [FromQuery] string? collegeId, [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo, [FromQuery] string? academicYear)
    {
        Guid.TryParse(collegeId, out var cid);
        DateTime.TryParse(dateFrom, out var dFrom);
        DateTime.TryParse(dateTo, out var dTo);

        var bytes = await _orchestrator.ExportCollegeWiseExcel(
            cid == Guid.Empty ? null : cid,
            dFrom == DateTime.MinValue ? null : dFrom,
            dTo == DateTime.MinValue ? null : dTo,
            academicYear);

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "college-wise-report.xlsx");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  COLLEGE ADMIN: BRANCH-WISE REPORT
    // ═══════════════════════════════════════════════════════════════════════

    [HttpGet("college-admin/branch-wise")]
    public async Task<IActionResult> GetBranchWiseReport(
        [FromQuery] string? collegeId, [FromQuery] string? classId,
        [FromQuery] string? batchId, [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo)
    {
        Guid.TryParse(collegeId, out var cid);
        Guid.TryParse(classId, out var clid);
        Guid.TryParse(batchId, out var bid);
        DateTime.TryParse(dateFrom, out var dFrom);
        DateTime.TryParse(dateTo, out var dTo);

        var report = await _orchestrator.GetBranchWiseReport(
            cid == Guid.Empty ? null : cid,
            clid == Guid.Empty ? null : clid,
            bid == Guid.Empty ? null : bid,
            dFrom == DateTime.MinValue ? null : dFrom,
            dTo == DateTime.MinValue ? null : dTo);

        return Ok(report);
    }

    [HttpGet("college-admin/branch-wise/export/pdf")]
    public async Task<IActionResult> ExportBranchWisePdf(
        [FromQuery] string? collegeId, [FromQuery] string? classId,
        [FromQuery] string? batchId, [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo)
    {
        Guid.TryParse(collegeId, out var cid);
        Guid.TryParse(classId, out var clid);
        Guid.TryParse(batchId, out var bid);
        DateTime.TryParse(dateFrom, out var dFrom);
        DateTime.TryParse(dateTo, out var dTo);

        var bytes = await _orchestrator.ExportBranchWisePdf(
            cid == Guid.Empty ? null : cid,
            clid == Guid.Empty ? null : clid,
            bid == Guid.Empty ? null : bid,
            dFrom == DateTime.MinValue ? null : dFrom,
            dTo == DateTime.MinValue ? null : dTo);

        return File(bytes, "application/pdf", "branch-wise-report.pdf");
    }

    [HttpGet("college-admin/branch-wise/export/excel")]
    public async Task<IActionResult> ExportBranchWiseExcel(
        [FromQuery] string? collegeId, [FromQuery] string? classId,
        [FromQuery] string? batchId, [FromQuery] string? dateFrom,
        [FromQuery] string? dateTo)
    {
        Guid.TryParse(collegeId, out var cid);
        Guid.TryParse(classId, out var clid);
        Guid.TryParse(batchId, out var bid);
        DateTime.TryParse(dateFrom, out var dFrom);
        DateTime.TryParse(dateTo, out var dTo);

        var bytes = await _orchestrator.ExportBranchWiseExcel(
            cid == Guid.Empty ? null : cid,
            clid == Guid.Empty ? null : clid,
            bid == Guid.Empty ? null : bid,
            dFrom == DateTime.MinValue ? null : dFrom,
            dTo == DateTime.MinValue ? null : dTo);

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "branch-wise-report.xlsx");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TRAINER: STUDENT PERFORMANCE REPORT
    // ═══════════════════════════════════════════════════════════════════════

    [HttpGet("trainer/student-performance")]
    public async Task<IActionResult> GetStudentPerformanceReport(
        [FromQuery] string? collegeId, [FromQuery] string? classId,
        [FromQuery] string? batchId, [FromQuery] string? studentId,
        [FromQuery] string? trainerId, [FromQuery] string? assessmentId,
        [FromQuery] string? dateFrom, [FromQuery] string? dateTo,
        [FromQuery] string? performanceLevel)
    {
        Guid.TryParse(collegeId, out var cid);
        Guid.TryParse(classId, out var clid);
        Guid.TryParse(batchId, out var bid);
        Guid.TryParse(studentId, out var sid);
        Guid.TryParse(trainerId, out var tid);
        Guid.TryParse(assessmentId, out var aid);
        DateTime.TryParse(dateFrom, out var dFrom);
        DateTime.TryParse(dateTo, out var dTo);

        var report = await _orchestrator.GetStudentPerformanceReport(
            cid == Guid.Empty ? null : cid,
            clid == Guid.Empty ? null : clid,
            bid == Guid.Empty ? null : bid,
            sid == Guid.Empty ? null : sid,
            tid == Guid.Empty ? null : tid,
            aid == Guid.Empty ? null : aid,
            dFrom == DateTime.MinValue ? null : dFrom,
            dTo == DateTime.MinValue ? null : dTo,
            performanceLevel);

        return Ok(report);
    }

    [HttpGet("trainer/student-performance/export/pdf")]
    public async Task<IActionResult> ExportStudentPerformancePdf(
        [FromQuery] string? collegeId, [FromQuery] string? classId,
        [FromQuery] string? batchId, [FromQuery] string? studentId,
        [FromQuery] string? trainerId, [FromQuery] string? assessmentId,
        [FromQuery] string? dateFrom, [FromQuery] string? dateTo,
        [FromQuery] string? performanceLevel)
    {
        Guid.TryParse(collegeId, out var cid);
        Guid.TryParse(classId, out var clid);
        Guid.TryParse(batchId, out var bid);
        Guid.TryParse(studentId, out var sid);
        Guid.TryParse(trainerId, out var tid);
        Guid.TryParse(assessmentId, out var aid);
        DateTime.TryParse(dateFrom, out var dFrom);
        DateTime.TryParse(dateTo, out var dTo);

        var bytes = await _orchestrator.ExportStudentPerformancePdf(
            cid == Guid.Empty ? null : cid,
            clid == Guid.Empty ? null : clid,
            bid == Guid.Empty ? null : bid,
            sid == Guid.Empty ? null : sid,
            tid == Guid.Empty ? null : tid,
            aid == Guid.Empty ? null : aid,
            dFrom == DateTime.MinValue ? null : dFrom,
            dTo == DateTime.MinValue ? null : dTo,
            performanceLevel);

        return File(bytes, "application/pdf", "student-performance-report.pdf");
    }

    [HttpGet("trainer/student-performance/export/excel")]
    public async Task<IActionResult> ExportStudentPerformanceExcel(
        [FromQuery] string? collegeId, [FromQuery] string? classId,
        [FromQuery] string? batchId, [FromQuery] string? studentId,
        [FromQuery] string? trainerId, [FromQuery] string? assessmentId,
        [FromQuery] string? dateFrom, [FromQuery] string? dateTo,
        [FromQuery] string? performanceLevel)
    {
        Guid.TryParse(collegeId, out var cid);
        Guid.TryParse(classId, out var clid);
        Guid.TryParse(batchId, out var bid);
        Guid.TryParse(studentId, out var sid);
        Guid.TryParse(trainerId, out var tid);
        Guid.TryParse(assessmentId, out var aid);
        DateTime.TryParse(dateFrom, out var dFrom);
        DateTime.TryParse(dateTo, out var dTo);

        var bytes = await _orchestrator.ExportStudentPerformanceExcel(
            cid == Guid.Empty ? null : cid,
            clid == Guid.Empty ? null : clid,
            bid == Guid.Empty ? null : bid,
            sid == Guid.Empty ? null : sid,
            tid == Guid.Empty ? null : tid,
            aid == Guid.Empty ? null : aid,
            dFrom == DateTime.MinValue ? null : dFrom,
            dTo == DateTime.MinValue ? null : dTo,
            performanceLevel);

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "student-performance-report.xlsx");
    }
}
