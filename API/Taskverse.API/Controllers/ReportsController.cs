using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Taskverse.Api.Models;
using Taskverse.Business.Interface;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : TaskverseBaseController
{
    private readonly IEmailService _emailService;
    private readonly IReportExportService _reportExportService;
    private readonly TaskverseContext _dbContext;

    public ReportsController(
        IEmailService emailService,
        IReportExportService reportExportService,
        TaskverseContext dbContext)
    {
        _emailService = emailService;
        _reportExportService = reportExportService;
        _dbContext = dbContext;
    }

    [HttpGet("context/trainer")]
    [Authorize(Roles = "Trainer")]
    public async Task<ActionResult<ReportContextResponse>> GetTrainerReportContext()
    {
        var trainer = await GetCurrentTrainer();
        if (trainer == null)
        {
            return Forbid();
        }

        var assignedBatchIds = await GetAssignedBatchIdsForTrainer(trainer.TrainerId);
        var context = await BuildReportContext(
            classesQuery: _dbContext.Classes
                .Where(c => c.CollegeId == trainer.CollegeId)
                .Where(c => _dbContext.Batches.Any(b => b.ClassId == c.ClassId && assignedBatchIds.Contains(b.BatchId))),
            batchesQuery: _dbContext.Batches
                .Where(b => b.CollegeId == trainer.CollegeId && assignedBatchIds.Contains(b.BatchId)),
            studentsQuery: _dbContext.Students
                .Where(s => s.CollegeId == trainer.CollegeId && s.BatchId.HasValue && assignedBatchIds.Contains(s.BatchId.Value)));

        return Ok(context);
    }

    [HttpGet("context/college-admin")]
    [Authorize(Roles = "CollegeAdmin")]
    public async Task<ActionResult<ReportContextResponse>> GetCollegeAdminReportContext()
    {
        if (!TryGetCollegeId(out var collegeId))
        {
            return Forbid();
        }

        var context = await BuildReportContext(
            classesQuery: _dbContext.Classes.Where(c => c.CollegeId == collegeId),
            batchesQuery: _dbContext.Batches.Where(b => b.CollegeId == collegeId),
            studentsQuery: _dbContext.Students.Where(s => s.CollegeId == collegeId));

        return Ok(context);
    }

    [HttpGet("context/student")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<ReportStudentResponse>> GetStudentReportContext()
    {
        if (!Guid.TryParse(UserId, out var userId))
        {
            return Forbid();
        }

        var student = await _dbContext.Students
            .Where(s => s.UserId == userId || s.StudentId == userId)
            .Select(s => new
            {
                s.StudentId,
                s.UserId,
                s.CollegeId,
                s.ClassId,
                s.BatchId,
                s.FullName,
                s.Email,
                s.EnrollmentNumber
            })
            .FirstOrDefaultAsync();

        if (student == null)
        {
            return NotFound(new { message = "Student report context not found." });
        }

        var resultStats = await GetStudentResultStats([student.StudentId]);
        resultStats.TryGetValue(student.StudentId, out var stats);

        return Ok(new ReportStudentResponse
        {
            StudentId = student.StudentId,
            UserId = student.UserId,
            CollegeId = student.CollegeId,
            ClassId = student.ClassId,
            BatchId = student.BatchId,
            FullName = student.FullName,
            Email = student.Email,
            EnrollmentNumber = student.EnrollmentNumber,
            AssessmentCount = stats.AssessmentCount,
            AveragePercentage = stats.AveragePercentage
        });
    }

    [HttpGet("classes/{classId}/batches/{batchId}/students")]
    [Authorize(Roles = "CollegeAdmin, Trainer")]
    public async Task<ActionResult<List<ReportStudentResponse>>> GetStudentsForBatch(Guid classId, Guid batchId)
    {
        var batch = await _dbContext.Batches
            .Where(b => b.BatchId == batchId && b.ClassId == classId)
            .Select(b => new { b.BatchId, b.ClassId, b.CollegeId })
            .FirstOrDefaultAsync();

        if (batch == null)
        {
            return NotFound(new { message = "Batch not found for the selected branch." });
        }

        if (UserRole == "CollegeAdmin")
        {
            if (!TryGetCollegeId(out var collegeId) || batch.CollegeId != collegeId)
            {
                return Forbid();
            }
        }
        else if (UserRole == "Trainer")
        {
            var trainer = await GetCurrentTrainer();
            if (trainer == null || trainer.CollegeId != batch.CollegeId)
            {
                return Forbid();
            }

            var ownsBatch = await _dbContext.TrainerBatches
                .AnyAsync(tb => tb.TrainerId == trainer.TrainerId && tb.BatchId == batch.BatchId);
            if (!ownsBatch)
            {
                return Forbid();
            }
        }
        else
        {
            return Forbid();
        }

        var students = await _dbContext.Students
            .Where(s => s.BatchId == batch.BatchId && s.Status == UserStatus.APPROVED)
            .OrderBy(s => s.FullName)
            .Select(s => new
            {
                s.StudentId,
                s.UserId,
                s.CollegeId,
                s.ClassId,
                s.BatchId,
                s.FullName,
                s.Email,
                s.EnrollmentNumber
            })
            .ToListAsync();

        var stats = await GetStudentResultStats(students.Select(s => s.StudentId).ToList());

        return Ok(students.Select(student =>
        {
            stats.TryGetValue(student.StudentId, out var stat);
            return new ReportStudentResponse
            {
                StudentId = student.StudentId,
                UserId = student.UserId,
                CollegeId = student.CollegeId,
                ClassId = student.ClassId,
                BatchId = student.BatchId,
                FullName = student.FullName,
                Email = student.Email,
                EnrollmentNumber = student.EnrollmentNumber,
                AssessmentCount = stat.AssessmentCount,
                AveragePercentage = stat.AveragePercentage
            };
        }).ToList());
    }

    [HttpGet("export/college/{collegeId}")]
    [Authorize(Roles = "SuperAdmin, CollegeAdmin")]
    public async Task<IActionResult> ExportCollegeReport(Guid collegeId, [FromQuery] string format = "pdf")
    {
        if (!await CanAccessCollege(collegeId))
        {
            return Forbid();
        }
        var report = await _reportExportService.GenerateCollegeReportAsync(collegeId, format);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("export/branch/{branchId}")]
    [Authorize(Roles = "SuperAdmin, CollegeAdmin, Trainer")]
    public async Task<IActionResult> ExportBranchReport(Guid branchId, [FromQuery] string format = "pdf")
    {
        if (!await CanAccessBranch(branchId))
        {
            return Forbid();
        }

        var report = await _reportExportService.GenerateBranchReportAsync(branchId, format);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("export/student/{studentId}")]
    [Authorize(Roles = "SuperAdmin, CollegeAdmin, Trainer, Student")]
    public async Task<IActionResult> ExportStudentReport(Guid studentId, [FromQuery] string format = "pdf")
    {
        if (!await CanAccessStudent(studentId))
        {
            return Forbid();
        }
        var report = await _reportExportService.GenerateStudentReportAsync(studentId, format);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpPost("export/email")]
    [Authorize(Roles = "SuperAdmin, CollegeAdmin, Trainer, Student")]
    public async Task<IActionResult> EmailReport([FromBody] ReportEmailRequest request)
    {
        var recipients = GetRecipients(request);
        if (recipients.Count == 0)
        {
            return BadRequest(new { message = "At least one recipient email is required." });
        }

        ReportExportFile report;
        switch (request.ReportType.ToLower())
        {
            case "college":
                if (!await CanAccessCollege(request.EntityId)) return Forbid();
                report = await _reportExportService.GenerateCollegeReportAsync(request.EntityId, request.Format);
                break;
            case "branch":
                if (!await CanAccessBranch(request.EntityId)) return Forbid();
                report = await _reportExportService.GenerateBranchReportAsync(request.EntityId, request.Format);
                break;
            case "student":
                if (!await CanAccessStudent(request.EntityId)) return Forbid();
                report = await _reportExportService.GenerateStudentReportAsync(request.EntityId, request.Format);
                break;
            default:
                return BadRequest(new { message = "Invalid report type" });
        }

        var message = new EmailMessage
        {
            ToAddresses = recipients,
            Subject = $"Your {request.ReportType} Report from Taskverse",
            HtmlBody = $"<p>Please find attached your requested {request.ReportType} report.</p>",
            Attachments = new List<EmailAttachment>
            {
                new EmailAttachment
                {
                    FileName = report.FileName,
                    Content = report.Content,
                    ContentType = report.ContentType
                }
            }
        };

        await _emailService.SendEmailAsync(message);

        return Ok(new { message = "Report emailed successfully" });
    }

    private async Task<ReportContextResponse> BuildReportContext(
        IQueryable<Taskverse.Data.DataAccess.Class> classesQuery,
        IQueryable<Batch> batchesQuery,
        IQueryable<Student> studentsQuery)
    {
        var classes = await classesQuery
            .OrderBy(c => c.Name)
            .Select(c => new { c.ClassId, c.CollegeId, c.Name })
            .ToListAsync();

        var batches = await batchesQuery
            .OrderBy(b => b.Name)
            .Select(b => new { b.BatchId, b.ClassId, b.CollegeId, b.Name })
            .ToListAsync();

        var students = await studentsQuery
            .Where(s => s.Status == UserStatus.APPROVED)
            .OrderBy(s => s.FullName)
            .Select(s => new
            {
                s.StudentId,
                s.UserId,
                s.CollegeId,
                s.ClassId,
                s.BatchId,
                s.FullName,
                s.Email,
                s.EnrollmentNumber
            })
            .ToListAsync();

        var resultStats = await GetStudentResultStats(students.Select(s => s.StudentId).ToList());
        var batchStudentCounts = students
            .Where(s => s.BatchId.HasValue)
            .GroupBy(s => s.BatchId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var totalResults = resultStats.Values.Sum(s => s.AssessmentCount);
        var weightedPercentage = totalResults > 0
            ? resultStats.Values.Sum(s => s.AveragePercentage * s.AssessmentCount) / totalResults
            : 0;

        return new ReportContextResponse
        {
            Totals = new ReportContextTotalsResponse
            {
                TotalClasses = classes.Count,
                TotalBatches = batches.Count,
                TotalStudents = students.Count,
                AveragePercentage = Math.Round(weightedPercentage, 1),
                PassRate = await GetPassRate(students.Select(s => s.StudentId).ToList())
            },
            Classes = classes.Select(classItem => new ReportClassResponse
            {
                ClassId = classItem.ClassId,
                CollegeId = classItem.CollegeId,
                Name = classItem.Name,
                Batches = batches
                    .Where(batch => batch.ClassId == classItem.ClassId)
                    .Select(batch => new ReportBatchResponse
                    {
                        BatchId = batch.BatchId,
                        ClassId = batch.ClassId,
                        CollegeId = batch.CollegeId,
                        Name = batch.Name,
                        StudentCount = batchStudentCounts.TryGetValue(batch.BatchId, out var count) ? count : 0,
                        Students = students
                            .Where(student => student.BatchId == batch.BatchId)
                            .Select(student =>
                            {
                                resultStats.TryGetValue(student.StudentId, out var stats);
                                return new ReportStudentResponse
                                {
                                    StudentId = student.StudentId,
                                    UserId = student.UserId,
                                    CollegeId = student.CollegeId,
                                    ClassId = student.ClassId,
                                    BatchId = student.BatchId,
                                    FullName = student.FullName,
                                    Email = student.Email,
                                    EnrollmentNumber = student.EnrollmentNumber,
                                    AssessmentCount = stats.AssessmentCount,
                                    AveragePercentage = stats.AveragePercentage
                                };
                            })
                            .ToList()
                    })
                    .ToList()
            }).ToList()
        };
    }

    private async Task<Dictionary<Guid, (int AssessmentCount, decimal AveragePercentage)>> GetStudentResultStats(IReadOnlyCollection<Guid> studentIds)
    {
        if (studentIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.Results
            .Where(r => studentIds.Contains(r.StudentId))
            .GroupBy(r => r.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                AssessmentCount = g.Count(),
                AveragePercentage = g.Average(r => r.Percentage)
            })
            .ToDictionaryAsync(x => x.StudentId, x => (x.AssessmentCount, Math.Round(x.AveragePercentage, 1)));
    }

    private async Task<decimal> GetPassRate(IReadOnlyCollection<Guid> studentIds)
    {
        if (studentIds.Count == 0)
        {
            return 0;
        }

        var results = await _dbContext.Results
            .Where(r => studentIds.Contains(r.StudentId))
            .Select(r => r.ResultStatus)
            .ToListAsync();

        return results.Count == 0
            ? 0
            : Math.Round((decimal)results.Count(status => status == ResultStatus.Pass) / results.Count * 100, 1);
    }

    private async Task<Trainer?> GetCurrentTrainer()
    {
        if (!Guid.TryParse(UserId, out var userId))
        {
            return null;
        }

        return await _dbContext.Trainers.FirstOrDefaultAsync(t => t.UserId == userId || t.TrainerId == userId);
    }

    private async Task<List<Guid>> GetAssignedBatchIdsForTrainer(Guid trainerId)
    {
        return await _dbContext.TrainerBatches
            .Where(tb => tb.TrainerId == trainerId)
            .Select(tb => tb.BatchId)
            .Distinct()
            .ToListAsync();
    }

    private bool TryGetCollegeId(out Guid collegeId)
    {
        return Guid.TryParse(CollegeId, out collegeId);
    }

    private async Task<bool> CanAccessCollege(Guid collegeId)
    {
        if (UserRole == "SuperAdmin")
        {
            return true;
        }

        return UserRole == "CollegeAdmin"
            && TryGetCollegeId(out var currentCollegeId)
            && currentCollegeId == collegeId;
    }

    private async Task<bool> CanAccessBranch(Guid branchId)
    {
        if (UserRole == "SuperAdmin")
        {
            return true;
        }

        var branch = await _dbContext.Classes
            .Where(c => c.ClassId == branchId)
            .Select(c => new { c.CollegeId })
            .FirstOrDefaultAsync();

        if (branch == null)
        {
            return false;
        }

        if (UserRole == "CollegeAdmin")
        {
            return TryGetCollegeId(out var collegeId) && branch.CollegeId == collegeId;
        }

        if (UserRole == "Trainer")
        {
            var trainer = await GetCurrentTrainer();
            if (trainer == null || trainer.CollegeId != branch.CollegeId)
            {
                return false;
            }

            var assignedBatchIds = await GetAssignedBatchIdsForTrainer(trainer.TrainerId);
            return await _dbContext.Batches.AnyAsync(b => b.ClassId == branchId && assignedBatchIds.Contains(b.BatchId));
        }

        return false;
    }

    private async Task<bool> CanAccessStudent(Guid studentId)
    {
        var student = await _dbContext.Students
            .Where(s => s.StudentId == studentId || s.UserId == studentId)
            .Select(s => new { s.UserId, s.CollegeId, s.BatchId })
            .FirstOrDefaultAsync();

        if (student == null)
        {
            return false;
        }

        if (UserRole == "SuperAdmin")
        {
            return true;
        }

        if (UserRole == "Student")
        {
            return Guid.TryParse(UserId, out var userId) && (student.UserId == userId || studentId == userId);
        }

        if (UserRole == "CollegeAdmin")
        {
            return TryGetCollegeId(out var collegeId) && student.CollegeId == collegeId;
        }

        if (UserRole == "Trainer")
        {
            var trainer = await GetCurrentTrainer();
            if (trainer == null || trainer.CollegeId != student.CollegeId || !student.BatchId.HasValue)
            {
                return false;
            }

            return await _dbContext.TrainerBatches
                .AnyAsync(tb => tb.TrainerId == trainer.TrainerId && tb.BatchId == student.BatchId.Value);
        }

        return false;
    }

    private static List<string> GetRecipients(ReportEmailRequest request)
    {
        return request.TargetEmails
            .Concat((request.TargetEmail ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(email => email.Trim())
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
