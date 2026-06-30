using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Taskverse.Business.Interface;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.Business.Services;

public class ReportExportService : IReportExportService
{
    private const string PdfContentType = "application/pdf";
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly TaskverseContext _context;

    public ReportExportService(TaskverseContext context)
    {
        _context = context;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<ReportExportFile> GenerateCollegeReportAsync(Guid collegeId, string format, CancellationToken cancellationToken = default)
    {
        var college = await _context.Colleges.FirstOrDefaultAsync(c => c.CollegeId == collegeId, cancellationToken)
            ?? throw new KeyNotFoundException("College not found.");
        var rows = await GetRowsForCollege(collegeId, cancellationToken);
        return BuildFile("College Report", college.CollegeName ?? "N/A", $"CollegeReport_{collegeId}", rows, format);
    }

    public async Task<ReportExportFile> GenerateBranchReportAsync(Guid classId, string format, CancellationToken cancellationToken = default)
    {
        var branch = await _context.Classes
            .FirstOrDefaultAsync(c => c.ClassId == classId, cancellationToken)
            ?? throw new KeyNotFoundException("Branch not found.");
        var rows = await GetRowsForBranch(classId, cancellationToken);
        return BuildFile("Branch Report", branch.Name, $"BranchReport_{classId}", rows, format);
    }

    public async Task<ReportExportFile> GenerateStudentReportAsync(Guid studentId, string format, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == studentId || s.UserId == studentId, cancellationToken)
            ?? throw new KeyNotFoundException("Student not found.");
        var rows = await GetRowsForStudents([student.StudentId], cancellationToken);
        return BuildFile("Student Report", student.FullName, $"StudentReport_{student.StudentId}", rows, format);
    }

    private async Task<List<ReportRow>> GetRowsForCollege(Guid collegeId, CancellationToken cancellationToken)
    {
        var classIds = await _context.Classes
            .Where(c => c.CollegeId == collegeId)
            .Select(c => c.ClassId)
            .ToListAsync(cancellationToken);
        var batchIds = await _context.Batches
            .Where(b => classIds.Contains(b.ClassId))
            .Select(b => b.BatchId)
            .ToListAsync(cancellationToken);
        var studentIds = await _context.Students
            .Where(s => s.BatchId.HasValue && batchIds.Contains(s.BatchId.Value))
            .Select(s => s.StudentId)
            .ToListAsync(cancellationToken);
        return await GetRowsForStudents(studentIds, cancellationToken);
    }

    private async Task<List<ReportRow>> GetRowsForBranch(Guid classId, CancellationToken cancellationToken)
    {
        var batchIds = await _context.Batches
            .Where(b => b.ClassId == classId)
            .Select(b => b.BatchId)
            .ToListAsync(cancellationToken);
        var studentIds = await _context.Students
            .Where(s => s.BatchId.HasValue && batchIds.Contains(s.BatchId.Value))
            .Select(s => s.StudentId)
            .ToListAsync(cancellationToken);
        return await GetRowsForStudents(studentIds, cancellationToken);
    }

    private async Task<List<ReportRow>> GetRowsForStudents(IReadOnlyCollection<Guid> studentIds, CancellationToken cancellationToken)
    {
        if (studentIds.Count == 0)
        {
            return [];
        }

        return await (from student in _context.Students
                      where studentIds.Contains(student.StudentId)
                      join batch in _context.Batches on student.BatchId equals batch.BatchId into batchJoin
                      from batch in batchJoin.DefaultIfEmpty()
                      join branch in _context.Classes on batch.ClassId equals branch.ClassId into branchJoin
                      from branch in branchJoin.DefaultIfEmpty()
                      join result in _context.Results on student.StudentId equals result.StudentId into resultJoin
                      from result in resultJoin.DefaultIfEmpty()
                      join assessment in _context.Assessments on result.AssessmentId equals assessment.AssessmentId into assessmentJoin
                      from assessment in assessmentJoin.DefaultIfEmpty()
                      join attempt in _context.Attempts on result.AttemptId equals attempt.AttemptId into attemptJoin
                      from attempt in attemptJoin.DefaultIfEmpty()
                      orderby student.FullName, assessment.AssessmentName
                      select new ReportRow(
                          student.FullName,
                          student.Email,
                          student.EnrollmentNumber ?? "N/A",
                          branch != null ? branch.Name : "N/A",
                          batch != null ? batch.Name : "N/A",
                          assessment != null ? assessment.AssessmentName : "No completed assessments",
                          result != null ? result.TotalMarks : 0,
                          result != null ? result.ObtainedMarks : 0,
                          result != null ? result.Percentage : 0,
                          result != null ? result.Rank : 0,
                          result != null ? result.ResultStatus.ToString() : "N/A",
                          attempt != null ? attempt.SubmittedAt : null,
                          result != null ? result.GeneratedAt : null))
            .ToListAsync(cancellationToken);
    }

    private static ReportExportFile BuildFile(string title, string filter, string baseFileName, List<ReportRow> rows, string format)
    {
        var normalizedFormat = string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase) ? "excel" : "pdf";
        return normalizedFormat == "excel"
            ? new ReportExportFile($"{baseFileName}.xlsx", ExcelContentType, BuildExcel(title, filter, rows))
            : new ReportExportFile($"{baseFileName}.pdf", PdfContentType, BuildPdf(title, filter, rows));
    }

    private static byte[] BuildExcel(string title, string filter, List<ReportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Report");
        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, 12).Merge().Style.Font.SetBold().Font.FontSize = 16;
        ws.Cell(2, 1).Value = $"Filter: {filter}";
        ws.Range(2, 1, 2, 12).Merge();
        ws.Cell(3, 1).Value = $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
        ws.Range(3, 1, 3, 12).Merge();

        var headers = new[] { "Student", "Email", "Enrollment", "Branch", "Batch", "Assessment", "Total Marks", "Obtained Marks", "Percentage", "Rank", "Status", "Submitted At" };
        for (var index = 0; index < headers.Length; index++)
        {
            var cell = ws.Cell(5, index + 1);
            cell.Value = headers[index];
            cell.Style.Font.SetBold().Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.DarkBlue;
        }

        var rowIndex = 6;
        foreach (var row in rows)
        {
            ws.Cell(rowIndex, 1).Value = row.StudentName;
            ws.Cell(rowIndex, 2).Value = row.Email;
            ws.Cell(rowIndex, 3).Value = row.EnrollmentNumber;
            ws.Cell(rowIndex, 4).Value = row.BranchName;
            ws.Cell(rowIndex, 5).Value = row.BatchName;
            ws.Cell(rowIndex, 6).Value = row.AssessmentName;
            ws.Cell(rowIndex, 7).Value = row.TotalMarks;
            ws.Cell(rowIndex, 8).Value = row.ObtainedMarks;
            ws.Cell(rowIndex, 9).Value = row.Percentage / 100;
            ws.Cell(rowIndex, 9).Style.NumberFormat.Format = "0.0%";
            ws.Cell(rowIndex, 10).Value = row.Rank == 0 ? "N/A" : row.Rank;
            ws.Cell(rowIndex, 11).Value = row.Status;
            ws.Cell(rowIndex, 12).Value = row.SubmittedAt?.ToString("yyyy-MM-dd HH:mm") ?? "N/A";
            rowIndex++;
        }

        ws.SheetView.FreezeRows(5);
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildPdf(string title, string filter, List<ReportRow> rows)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(style => style.FontSize(9).FontFamily(Fonts.Arial));
                page.Header().Column(header =>
                {
                    header.Item().Text(title).FontSize(18).SemiBold().FontColor(Colors.Blue.Darken2);
                    header.Item().Text($"Filter: {filter}").FontSize(10);
                    header.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    foreach (var heading in new[] { "Student", "Enrollment", "Branch", "Batch", "Assessment", "Marks", "Percentage", "Status" })
                    {
                        table.Cell().Background(Colors.Blue.Darken2).Padding(4).Text(heading).FontColor(Colors.White).SemiBold();
                    }

                    foreach (var row in rows)
                    {
                        table.Cell().Padding(4).Text(row.StudentName);
                        table.Cell().Padding(4).Text(row.EnrollmentNumber);
                        table.Cell().Padding(4).Text(row.BranchName);
                        table.Cell().Padding(4).Text(row.BatchName);
                        table.Cell().Padding(4).Text(row.AssessmentName);
                        table.Cell().Padding(4).Text($"{row.ObtainedMarks:F1}/{row.TotalMarks:F1}");
                        table.Cell().Padding(4).Text($"{row.Percentage:F1}%");
                        table.Cell().Padding(4).Text(row.Status);
                    }
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    private sealed record ReportRow(
        string StudentName,
        string Email,
        string EnrollmentNumber,
        string BranchName,
        string BatchName,
        string AssessmentName,
        decimal TotalMarks,
        decimal ObtainedMarks,
        decimal Percentage,
        int Rank,
        string Status,
        DateTime? SubmittedAt,
        DateTime? GeneratedAt);
}
