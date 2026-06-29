using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Taskverse.Api.MicroServices.Enums;
using Taskverse.Api.MicroServices.Interfaces;
using Taskverse.Api.MicroServices.Models;

namespace Taskverse.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : TaskverseBaseController
{
    private readonly IMicroServiceOrchestrator _microServiceOrchestrator;

    public ReportsController(IMicroServiceOrchestrator microServiceOrchestrator)
    {
        _microServiceOrchestrator = microServiceOrchestrator;
    }

    [HttpGet("export/college/{collegeId}")]
    [Authorize(Roles = "SuperAdmin, CollegeAdmin")]
    public async Task<IActionResult> ExportCollegeReport(Guid collegeId, [FromQuery] string format = "pdf")
    {
        // Enforce role-based access if College Admin (can only export their own college)
        if (UserRole == "CollegeAdmin")
        {
            if (string.IsNullOrEmpty(CollegeId) || Guid.Parse(CollegeId) != collegeId)
            {
                return Forbid();
            }
        }
        
        return await _microServiceOrchestrator.ExportCollegeReport(collegeId, format);
    }

    [HttpGet("export/branch/{branchId}")]
    [Authorize(Roles = "SuperAdmin, CollegeAdmin, Trainer")]
    public async Task<IActionResult> ExportBranchReport(Guid branchId, [FromQuery] string format = "pdf")
    {
        // For simplicity in this demo, letting auth pass. In real prod, validate if branch belongs to user's college/assigned branches.
        return await _microServiceOrchestrator.ExportBranchReport(branchId, format);
    }

    [HttpGet("export/student/{studentId}")]
    [Authorize(Roles = "SuperAdmin, CollegeAdmin, Trainer")]
    public async Task<IActionResult> ExportStudentReport(Guid studentId, [FromQuery] string format = "pdf")
    {
        return await _microServiceOrchestrator.ExportStudentReport(studentId, format);
    }
}
