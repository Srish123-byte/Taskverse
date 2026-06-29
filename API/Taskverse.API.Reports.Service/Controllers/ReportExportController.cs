using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Taskverse.API.Reports.Service.Managers;

namespace Taskverse.API.Reports.Service.Controllers;

[ApiController]
[Route("api/reports/export")]
public class ReportExportController : ControllerBase
{
    private readonly IReportExportManager _reportExportManager;

    public ReportExportController(IReportExportManager reportExportManager)
    {
        _reportExportManager = reportExportManager;
    }

    [HttpGet("college/{collegeId}")]
    public async Task<IActionResult> ExportCollegeReport(Guid collegeId, [FromQuery] string format = "pdf")
    {
        try
        {
            if (format.ToLower() == "excel")
            {
                var fileBytes = await _reportExportManager.GenerateCollegeWiseExcelAsync(collegeId);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"CollegeReport_{collegeId}.xlsx");
            }
            else
            {
                var fileBytes = await _reportExportManager.GenerateCollegeWisePdfAsync(collegeId);
                return File(fileBytes, "application/pdf", $"CollegeReport_{collegeId}.pdf");
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("branch/{branchId}")]
    public async Task<IActionResult> ExportBranchReport(Guid branchId, [FromQuery] string format = "pdf")
    {
        try
        {
            if (format.ToLower() == "excel")
            {
                var fileBytes = await _reportExportManager.GenerateBranchWiseExcelAsync(branchId);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BranchReport_{branchId}.xlsx");
            }
            else
            {
                var fileBytes = await _reportExportManager.GenerateBranchWisePdfAsync(branchId);
                return File(fileBytes, "application/pdf", $"BranchReport_{branchId}.pdf");
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> ExportStudentReport(Guid studentId, [FromQuery] string format = "pdf")
    {
        try
        {
            if (format.ToLower() == "excel")
            {
                var fileBytes = await _reportExportManager.GenerateStudentExcelAsync(studentId);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"StudentReport_{studentId}.xlsx");
            }
            else
            {
                var fileBytes = await _reportExportManager.GenerateStudentPdfAsync(studentId);
                return File(fileBytes, "application/pdf", $"StudentReport_{studentId}.pdf");
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
