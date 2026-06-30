using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Reports.Service.Managers;

public class ReportExportManager : IReportExportManager
{
    private readonly TaskverseContext _dbContext;

    public ReportExportManager(TaskverseContext dbContext)
    {
        _dbContext = dbContext;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private void BuildPdfHeader(PageDescriptor page, string title, string institutionName)
    {
        page.Header().PaddingBottom(10).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(title).FontSize(24).SemiBold().FontColor(Colors.Blue.Darken2);
                col.Item().Text($"Institution: {institutionName ?? "N/A"}").FontSize(14).FontColor(Colors.Grey.Darken2);
                col.Item().Text($"Generated On: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(10).FontColor(Colors.Grey.Medium);
            });
            row.ConstantItem(100).AlignRight().Text("TASKVERSE").FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
        });
    }

    private void BuildPdfFooter(PageDescriptor page)
    {
        page.Footer().AlignCenter().Text(x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
            x.Span(" of ");
            x.TotalPages();
        });
    }

    private void StyleExcelHeader(IXLWorksheet ws, int columns, string title, string subtitle)
    {
        ws.Cell(1, 1).Value = "TASKVERSE REPORT";
        ws.Range(1, 1, 1, columns).Merge().Style.Font.SetBold().Font.FontSize = 16;
        ws.Range(1, 1, 1, columns).Style.Fill.BackgroundColor = XLColor.DarkBlue;
        ws.Range(1, 1, 1, columns).Style.Font.FontColor = XLColor.White;

        ws.Cell(2, 1).Value = title;
        ws.Range(2, 1, 2, columns).Merge().Style.Font.SetBold().Font.FontSize = 14;

        ws.Cell(3, 1).Value = subtitle;
        ws.Range(3, 1, 3, columns).Merge().Style.Font.FontSize = 12;
        ws.Cell(4, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
        ws.Range(4, 1, 4, columns).Merge().Style.Font.FontSize = 10;
        
        ws.SheetView.FreezeRows(5); // Freeze rows above headers
    }

    public async Task<byte[]> GenerateCollegeWisePdfAsync(Guid collegeId, CancellationToken cancellationToken = default)
    {
        var college = await _dbContext.Colleges.FirstOrDefaultAsync(c => c.CollegeId == collegeId, cancellationToken);
        if (college == null) throw new Exception("College not found");

        var collegeClassIds = await _dbContext.Classes
            .Where(c => c.CollegeId == collegeId)
            .Select(c => c.ClassId)
            .ToListAsync(cancellationToken);
        var collegeBatchIds = await _dbContext.Batches
            .Where(b => collegeClassIds.Contains(b.ClassId))
            .Select(b => b.BatchId)
            .ToListAsync(cancellationToken);

        var totalStudents = await _dbContext.Students.CountAsync(s => s.BatchId.HasValue && collegeBatchIds.Contains(s.BatchId.Value), cancellationToken);
        var totalBranches = collegeClassIds.Count;
        var totalTrainers = await _dbContext.Trainers.CountAsync(t => t.CollegeId == collegeId, cancellationToken);

        var results = await (from r in _dbContext.Results
                             join s in _dbContext.Students on r.StudentId equals s.StudentId
                             where s.BatchId.HasValue && collegeBatchIds.Contains(s.BatchId.Value)
                             select r).ToListAsync(cancellationToken);

        var averagePercentage = results.Any() ? results.Average(r => r.Percentage) : 0;
        var passPercentage = results.Any() ? (decimal)results.Count(r => r.ResultStatus == Taskverse.Data.Enums.ResultStatus.Pass) / results.Count * 100 : 0;

        var studentStats = await (from s in _dbContext.Students
                                  where s.BatchId.HasValue && collegeBatchIds.Contains(s.BatchId.Value)
                                  join b in _dbContext.Batches on s.BatchId equals b.BatchId
                                  join c in _dbContext.Classes on b.ClassId equals c.ClassId
                                  select new
                                  {
                                      USN = s.EnrollmentNumber ?? "N/A",
                                      Name = s.FullName,
                                      Branch = c.Name,
                                      StudentId = s.StudentId
                                  }).ToListAsync(cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                BuildPdfHeader(page, "Executive Summary", college.CollegeName ?? "N/A");

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().Background(Colors.Blue.Darken3).Padding(20).Column(cover => 
                    {
                        cover.Item().Text("COLLEGE PERFORMANCE INSIGHTS").FontSize(24).Bold().FontColor(Colors.White);
                        cover.Item().Text(college.CollegeName).FontSize(16).FontColor(Colors.Grey.Lighten2);
                        cover.Item().PaddingTop(10).Text($"Generated: {DateTime.Now:MMMM dd, yyyy}").FontSize(10).FontColor(Colors.White);
                    });

                    col.Spacing(15);
                    col.Item().PaddingTop(15).Text("Key Performance Indicators").FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                    
                    // Summary Cards
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        void AddCard(string title, string value)
                        {
                            table.Cell().Padding(5).Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c =>
                            {
                                c.Item().AlignCenter().Text(title).FontSize(9).FontColor(Colors.Grey.Darken2).SemiBold();
                                c.Item().AlignCenter().Text(value).FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                            });
                        }

                        AddCard("Total Students", totalStudents.ToString());
                        AddCard("Total Branches", totalBranches.ToString());
                        AddCard("Average %", $"{averagePercentage:F1}%");
                        AddCard("Pass %", $"{passPercentage:F1}%");
                    });

                    // Data Table
                    col.Item().PaddingBottom(5).PaddingTop(10).Text("Student Performance Roster").FontSize(14).SemiBold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(); // USN
                            columns.RelativeColumn(2); // Name
                            columns.RelativeColumn(); // Branch
                            columns.RelativeColumn(); // Assessments
                            columns.RelativeColumn(); // Avg Marks
                            columns.RelativeColumn(); // Percentage
                            columns.RelativeColumn(); // Status
                        });

                        table.Header(header =>
                        {
                            string[] headers = { "USN", "Name", "Branch", "Assessments", "Avg Marks", "Percentage", "Status" };
                            foreach (var h in headers)
                            {
                                header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text(h).FontColor(Colors.White).SemiBold();
                            }
                        });

                        bool alt = false;
                        foreach (var stat in studentStats)
                        {
                            var stuResults = results.Where(r => r.StudentId == stat.StudentId).ToList();
                            var bg = alt ? Colors.Grey.Lighten4 : Colors.White;

                            table.Cell().Background(bg).Padding(5).Text(stat.USN);
                            table.Cell().Background(bg).Padding(5).Text(stat.Name);
                            table.Cell().Background(bg).Padding(5).Text(stat.Branch);
                            table.Cell().Background(bg).Padding(5).Text(stuResults.Count.ToString());
                            table.Cell().Background(bg).Padding(5).Text(stuResults.Any() ? stuResults.Average(r => r.ObtainedMarks).ToString("F1") : "0");
                            table.Cell().Background(bg).Padding(5).Text(stuResults.Any() ? stuResults.Average(r => r.Percentage).ToString("F1") + "%" : "0%");
                            var status = stuResults.OrderByDescending(r => r.GeneratedAt).Select(r => r.ResultStatus.ToString()).FirstOrDefault() ?? "N/A";
                            table.Cell().Background(bg).Padding(5).Text(status).FontColor(status == "Pass" ? Colors.Green.Darken2 : Colors.Red.Darken2).SemiBold();
                            
                            alt = !alt;
                        }
                    });
                });

                BuildPdfFooter(page);
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerateCollegeWiseExcelAsync(Guid collegeId, CancellationToken cancellationToken = default)
    {
        var college = await _dbContext.Colleges.FirstOrDefaultAsync(c => c.CollegeId == collegeId, cancellationToken);
        if (college == null) throw new Exception("College not found");

        var collegeClassIds = await _dbContext.Classes
            .Where(c => c.CollegeId == collegeId)
            .Select(c => c.ClassId)
            .ToListAsync(cancellationToken);
        var collegeBatchIds = await _dbContext.Batches
            .Where(b => collegeClassIds.Contains(b.ClassId))
            .Select(b => b.BatchId)
            .ToListAsync(cancellationToken);

        var studentStats = await (from s in _dbContext.Students
                                  where s.BatchId.HasValue && collegeBatchIds.Contains(s.BatchId.Value)
                                  join b in _dbContext.Batches on s.BatchId equals b.BatchId
                                  join c in _dbContext.Classes on b.ClassId equals c.ClassId
                                  select new { s.StudentId, s.EnrollmentNumber, s.FullName, BranchName = c.Name })
                                  .ToListAsync(cancellationToken);

        var results = await (from r in _dbContext.Results
                             join s in _dbContext.Students on r.StudentId equals s.StudentId
                             where s.BatchId.HasValue && collegeBatchIds.Contains(s.BatchId.Value)
                             select r).ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        
        // --- SUMMARY SHEET ---
        var wsSummary = workbook.Worksheets.Add("Executive Summary");
        wsSummary.Cell(2, 2).Value = "COLLEGE PERFORMANCE INSIGHTS";
        wsSummary.Range(2, 2, 2, 6).Merge().Style.Font.SetBold().Font.FontSize = 20;
        wsSummary.Range(2, 2, 2, 6).Style.Font.FontColor = XLColor.DarkBlue;
        
        wsSummary.Cell(3, 2).Value = $"Institution: {college.CollegeName}";
        wsSummary.Range(3, 2, 3, 6).Merge().Style.Font.FontSize = 14;
        
        wsSummary.Cell(5, 2).Value = "Total Students";
        wsSummary.Cell(5, 3).Value = studentStats.Count;
        wsSummary.Cell(6, 2).Value = "Average Percentage";
        wsSummary.Cell(6, 3).Value = results.Any() ? results.Average(r => r.Percentage) / 100 : 0;
        wsSummary.Cell(6, 3).Style.NumberFormat.Format = "0.0%";
        
        wsSummary.Range(5, 2, 6, 2).Style.Font.SetBold().Fill.BackgroundColor = XLColor.AliceBlue;
        wsSummary.Range(5, 2, 6, 3).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        wsSummary.Columns().AdjustToContents();

        // --- DATA SHEET ---
        var ws = workbook.Worksheets.Add("Student Roster");
        
        StyleExcelHeader(ws, 7, "Student Performance Roster", $"Institution: {college.CollegeName}");

        var headers = new[] { "USN", "Student Name", "Branch", "Assessment Count", "Average Marks", "Percentage", "Status" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(5, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold().Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        int row = 6;
        foreach (var stat in studentStats)
        {
            var stuResults = results.Where(r => r.StudentId == stat.StudentId).ToList();
            ws.Cell(row, 1).Value = stat.EnrollmentNumber ?? "N/A";
            ws.Cell(row, 2).Value = stat.FullName;
            ws.Cell(row, 3).Value = stat.BranchName;
            ws.Cell(row, 4).Value = stuResults.Count;
            ws.Cell(row, 5).Value = stuResults.Any() ? stuResults.Average(r => r.ObtainedMarks) : 0;
            
            var pct = stuResults.Any() ? stuResults.Average(r => r.Percentage) / 100 : 0;
            ws.Cell(row, 6).Value = pct;
            ws.Cell(row, 6).Style.NumberFormat.Format = "0.0%";
            
            var status = stuResults.OrderByDescending(r => r.GeneratedAt).Select(r => r.ResultStatus.ToString()).FirstOrDefault() ?? "N/A";
            ws.Cell(row, 7).Value = status;
            
            if (status == "Pass") ws.Cell(row, 7).Style.Font.FontColor = XLColor.Green;
            else if (status == "Fail") ws.Cell(row, 7).Style.Font.FontColor = XLColor.Red;
            
            if (row % 2 == 0) ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.AliceBlue;
            
            ws.Range(row, 1, row, 7).Style.Border.SetBottomBorder(XLBorderStyleValues.Thin).Border.SetBottomBorderColor(XLColor.LightGray);
            row++;
        }

        ws.Columns().AdjustToContents();
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerateBranchWisePdfAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var branch = await _dbContext.Classes.FirstOrDefaultAsync(c => c.ClassId == branchId, cancellationToken);
        if (branch == null) throw new Exception("Branch not found");
        var college = await _dbContext.Colleges.FirstOrDefaultAsync(c => c.CollegeId == branch.CollegeId, cancellationToken);

        var branchBatchIds = await _dbContext.Batches
            .Where(b => b.ClassId == branchId)
            .Select(b => b.BatchId)
            .ToListAsync(cancellationToken);
        var students = await _dbContext.Students
            .Where(s => s.BatchId.HasValue && branchBatchIds.Contains(s.BatchId.Value))
            .ToListAsync(cancellationToken);
        var studentIds = students.Select(s => s.StudentId).ToList();
        var results = await _dbContext.Results.Where(r => studentIds.Contains(r.StudentId)).ToListAsync(cancellationToken);

        var totalStudents = students.Count;
        var averagePercentage = results.Any() ? results.Average(r => r.Percentage) : 0;
        var passPercentage = results.Any() ? (decimal)results.Count(r => r.ResultStatus == Taskverse.Data.Enums.ResultStatus.Pass) / results.Count * 100 : 0;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                BuildPdfHeader(page, "Executive Summary", college?.CollegeName ?? "N/A");

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().Background(Colors.Blue.Darken3).Padding(20).Column(cover => 
                    {
                        cover.Item().Text("BRANCH PERFORMANCE INSIGHTS").FontSize(24).Bold().FontColor(Colors.White);
                        cover.Item().Text($"Branch: {branch.Name}").FontSize(16).FontColor(Colors.Grey.Lighten2);
                        cover.Item().PaddingTop(10).Text($"Generated: {DateTime.Now:MMMM dd, yyyy}").FontSize(10).FontColor(Colors.White);
                    });

                    col.Spacing(15);
                    col.Item().PaddingTop(15).Text("Key Performance Indicators").FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                    
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); });
                        table.Cell().Padding(5).Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c => { c.Item().AlignCenter().Text("Total Students").FontSize(9).FontColor(Colors.Grey.Darken2).SemiBold(); c.Item().AlignCenter().Text(totalStudents.ToString()).FontSize(16).Bold().FontColor(Colors.Blue.Darken2); });
                        table.Cell().Padding(5).Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c => { c.Item().AlignCenter().Text("Average %").FontSize(9).FontColor(Colors.Grey.Darken2).SemiBold(); c.Item().AlignCenter().Text($"{averagePercentage:F1}%").FontSize(16).Bold().FontColor(Colors.Blue.Darken2); });
                        table.Cell().Padding(5).Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c => { c.Item().AlignCenter().Text("Pass %").FontSize(9).FontColor(Colors.Grey.Darken2).SemiBold(); c.Item().AlignCenter().Text($"{passPercentage:F1}%").FontSize(16).Bold().FontColor(Colors.Blue.Darken2); });
                    });

                    col.Item().PaddingBottom(5).PaddingTop(10).Text("Student Performance Table").FontSize(14).SemiBold();
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(); 
                            columns.RelativeColumn(2); 
                            columns.RelativeColumn(); 
                            columns.RelativeColumn(); 
                            columns.RelativeColumn(); 
                            columns.RelativeColumn(); 
                        });

                        table.Header(header =>
                        {
                            string[] headers = { "USN", "Name", "Average Marks", "Percentage", "Rank", "Status" };
                            foreach (var h in headers) header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text(h).FontColor(Colors.White).SemiBold();
                        });

                        bool alt = false;
                        foreach (var stat in students)
                        {
                            var stuResults = results.Where(r => r.StudentId == stat.StudentId).ToList();
                            var bg = alt ? Colors.Grey.Lighten4 : Colors.White;

                            table.Cell().Background(bg).Padding(5).Text(stat.EnrollmentNumber ?? "N/A");
                            table.Cell().Background(bg).Padding(5).Text(stat.FullName);
                            table.Cell().Background(bg).Padding(5).Text(stuResults.Any() ? stuResults.Average(r => r.ObtainedMarks).ToString("F1") : "0");
                            table.Cell().Background(bg).Padding(5).Text(stuResults.Any() ? stuResults.Average(r => r.Percentage).ToString("F1") + "%" : "0%");
                            table.Cell().Background(bg).Padding(5).Text(stuResults.OrderByDescending(r => r.GeneratedAt).Select(r => r.Rank.ToString()).FirstOrDefault() ?? "N/A");
                            var status = stuResults.OrderByDescending(r => r.GeneratedAt).Select(r => r.ResultStatus.ToString()).FirstOrDefault() ?? "N/A";
                            table.Cell().Background(bg).Padding(5).Text(status).FontColor(status == "Pass" ? Colors.Green.Darken2 : Colors.Red.Darken2).SemiBold();
                            
                            alt = !alt;
                        }
                    });
                });
                BuildPdfFooter(page);
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerateBranchWiseExcelAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var branch = await _dbContext.Classes.FirstOrDefaultAsync(c => c.ClassId == branchId, cancellationToken);
        if (branch == null) throw new Exception("Branch not found");
        var college = await _dbContext.Colleges.FirstOrDefaultAsync(c => c.CollegeId == branch.CollegeId, cancellationToken);

        var branchBatchIds = await _dbContext.Batches
            .Where(b => b.ClassId == branchId)
            .Select(b => b.BatchId)
            .ToListAsync(cancellationToken);
        var students = await _dbContext.Students
            .Where(s => s.BatchId.HasValue && branchBatchIds.Contains(s.BatchId.Value))
            .ToListAsync(cancellationToken);
        var studentIds = students.Select(s => s.StudentId).ToList();
        var results = await _dbContext.Results.Where(r => studentIds.Contains(r.StudentId)).ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        
        // --- SUMMARY SHEET ---
        var wsSummary = workbook.Worksheets.Add("Executive Summary");
        wsSummary.Cell(2, 2).Value = "BRANCH PERFORMANCE INSIGHTS";
        wsSummary.Range(2, 2, 2, 6).Merge().Style.Font.SetBold().Font.FontSize = 20;
        wsSummary.Range(2, 2, 2, 6).Style.Font.FontColor = XLColor.DarkBlue;
        
        wsSummary.Cell(3, 2).Value = $"Branch: {branch.Name} | Institution: {college?.CollegeName ?? "N/A"}";
        wsSummary.Range(3, 2, 3, 6).Merge().Style.Font.FontSize = 14;
        
        wsSummary.Cell(5, 2).Value = "Total Students";
        wsSummary.Cell(5, 3).Value = students.Count;
        wsSummary.Cell(6, 2).Value = "Average Percentage";
        wsSummary.Cell(6, 3).Value = results.Any() ? results.Average(r => r.Percentage) / 100 : 0;
        wsSummary.Cell(6, 3).Style.NumberFormat.Format = "0.0%";
        
        wsSummary.Range(5, 2, 6, 2).Style.Font.SetBold().Fill.BackgroundColor = XLColor.AliceBlue;
        wsSummary.Range(5, 2, 6, 3).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        wsSummary.Columns().AdjustToContents();

        // --- DATA SHEET ---
        var ws = workbook.Worksheets.Add("Student Roster");
        
        StyleExcelHeader(ws, 6, $"Branch Wise Performance Report - {branch.Name}", $"Institution: {college?.CollegeName ?? "N/A"}");

        var headers = new[] { "USN", "Student Name", "Average Marks", "Percentage", "Rank", "Status" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(5, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold().Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        int row = 6;
        foreach (var stat in students)
        {
            var stuResults = results.Where(r => r.StudentId == stat.StudentId).ToList();
            ws.Cell(row, 1).Value = stat.EnrollmentNumber ?? "N/A";
            ws.Cell(row, 2).Value = stat.FullName;
            ws.Cell(row, 3).Value = stuResults.Any() ? stuResults.Average(r => r.ObtainedMarks) : 0;
            
            var pct = stuResults.Any() ? stuResults.Average(r => r.Percentage) / 100 : 0;
            ws.Cell(row, 4).Value = pct;
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
            
            ws.Cell(row, 5).Value = stuResults.OrderByDescending(r => r.GeneratedAt).Select(r => r.Rank).FirstOrDefault();
            
            var status = stuResults.OrderByDescending(r => r.GeneratedAt).Select(r => r.ResultStatus.ToString()).FirstOrDefault() ?? "N/A";
            ws.Cell(row, 6).Value = status;
            
            if (status == "Pass") ws.Cell(row, 6).Style.Font.FontColor = XLColor.Green;
            else if (status == "Fail") ws.Cell(row, 6).Style.Font.FontColor = XLColor.Red;
            
            if (row % 2 == 0) ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.AliceBlue;
            
            ws.Range(row, 1, row, 6).Style.Border.SetBottomBorder(XLBorderStyleValues.Thin).Border.SetBottomBorderColor(XLColor.LightGray);
            row++;
        }

        ws.Columns().AdjustToContents();
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerateStudentPdfAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _dbContext.Students
            .Include(s => s.College)
            .Include(s => s.Class)
            .Include(s => s.Batch)
            .FirstOrDefaultAsync(s => s.StudentId == studentId, cancellationToken);
            
        if (student == null) throw new Exception("Student not found");

        var results = await (from r in _dbContext.Results
                             join a in _dbContext.Assessments on r.AssessmentId equals a.AssessmentId
                             join s in _dbContext.Subjects on a.SubjectId equals s.SubjectId into subj
                             from sub in subj.DefaultIfEmpty()
                             where r.StudentId == studentId
                             select new { r.ObtainedMarks, r.TotalMarks, r.Percentage, r.Rank, r.ResultStatus, SubjectName = sub != null ? sub.SubjectName : "General" })
                             .ToListAsync(cancellationToken);

        var overallPercentage = results.Any() ? results.Average(r => r.Percentage) : 0;
        var totalMarks = results.Sum(r => r.TotalMarks);
        var obtainedMarks = results.Sum(r => r.ObtainedMarks);
        var rank = results.Any() ? results.Min(r => r.Rank) : 0;
        var grade = overallPercentage >= 90 ? "A+" : overallPercentage >= 80 ? "A" : overallPercentage >= 70 ? "B" : overallPercentage >= 60 ? "C" : "F";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                BuildPdfHeader(page, "Executive Summary", student.College?.CollegeName ?? "N/A");

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Item().Background(Colors.Blue.Darken3).Padding(20).Column(cover => 
                    {
                        cover.Item().Text("STUDENT PERFORMANCE INSIGHTS").FontSize(24).Bold().FontColor(Colors.White);
                        cover.Item().Text(student.FullName).FontSize(18).FontColor(Colors.White).SemiBold();
                        cover.Item().Text($"USN: {student.EnrollmentNumber ?? "N/A"}").FontSize(14).FontColor(Colors.Grey.Lighten2);
                        cover.Item().PaddingTop(10).Text($"Generated: {DateTime.Now:MMMM dd, yyyy}").FontSize(10).FontColor(Colors.White);
                    });

                    col.Spacing(15);
                    col.Item().PaddingTop(15).Text("Key Performance Indicators").FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                    
                    // Overall Performance
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); });
                        table.Cell().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(c => { c.Item().AlignCenter().Text("Total Marks").FontSize(9).FontColor(Colors.Grey.Darken2).SemiBold(); c.Item().AlignCenter().Text($"{obtainedMarks:F1} / {totalMarks:F1}").FontSize(16).Bold().FontColor(Colors.Blue.Darken2); });
                        table.Cell().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(c => { c.Item().AlignCenter().Text("Percentage").FontSize(9).FontColor(Colors.Grey.Darken2).SemiBold(); c.Item().AlignCenter().Text($"{overallPercentage:F1}%").FontSize(16).Bold().FontColor(Colors.Blue.Darken2); });
                        table.Cell().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(c => { c.Item().AlignCenter().Text("Grade").FontSize(9).FontColor(Colors.Grey.Darken2).SemiBold(); c.Item().AlignCenter().Text(grade).FontSize(16).Bold().FontColor(Colors.Blue.Darken2); });
                        table.Cell().Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(c => { c.Item().AlignCenter().Text("Best Rank").FontSize(9).FontColor(Colors.Grey.Darken2).SemiBold(); c.Item().AlignCenter().Text(rank.ToString()).FontSize(16).Bold().FontColor(Colors.Blue.Darken2); });
                    });

                    // Assessment Breakdown
                    col.Item().PaddingTop(10).Text("Assessment Breakdown by Subject").SemiBold().FontSize(14);
                    
                    var groupedBySubject = results.GroupBy(r => r.SubjectName).ToList();
                    
                    if (!groupedBySubject.Any())
                    {
                        col.Item().Text("No sufficient assessment data available.").Italic().FontColor(Colors.Grey.Medium);
                    }
                    else
                    {
                        col.Item().Table(table => 
                        {
                            table.ColumnsDefinition(columns => { columns.RelativeColumn(2); columns.RelativeColumn(); columns.RelativeColumn(); });
                            table.Header(h => {
                                h.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Subject").FontColor(Colors.White).SemiBold();
                                h.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Average Marks").FontColor(Colors.White).SemiBold();
                                h.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Percentage").FontColor(Colors.White).SemiBold();
                            });
                            
                            bool alt = false;
                            foreach(var group in groupedBySubject)
                            {
                                var bg = alt ? Colors.Grey.Lighten4 : Colors.White;
                                table.Cell().Background(bg).Padding(5).Text(group.Key).SemiBold();
                                table.Cell().Background(bg).Padding(5).Text($"{group.Average(x => x.ObtainedMarks):F1} / {group.Average(x => x.TotalMarks):F1}");
                                table.Cell().Background(bg).Padding(5).Text($"{group.Average(x => x.Percentage):F1}%");
                                alt = !alt;
                            }
                        });
                        
                        col.Item().PaddingTop(10).Text("Performance Summary").SemiBold().FontSize(14);
                        var bestSubject = groupedBySubject.OrderByDescending(g => g.Average(x => x.Percentage)).FirstOrDefault()?.Key ?? "N/A";
                        var worstSubject = groupedBySubject.OrderBy(g => g.Average(x => x.Percentage)).FirstOrDefault()?.Key ?? "N/A";
                        
                        col.Item().Text($"Strengths: Excellent performance in {bestSubject}.").FontColor(Colors.Green.Darken2).SemiBold();
                        col.Item().Text($"Areas for Growth: Needs improvement in {worstSubject}.").FontColor(Colors.Orange.Darken2).SemiBold();
                    }
                });
                BuildPdfFooter(page);
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> GenerateStudentExcelAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _dbContext.Students
            .Include(s => s.College)
            .Include(s => s.Class)
            .Include(s => s.Batch)
            .FirstOrDefaultAsync(s => s.StudentId == studentId, cancellationToken);
            
        if (student == null) throw new Exception("Student not found");

        var results = await (from r in _dbContext.Results
                             join a in _dbContext.Assessments on r.AssessmentId equals a.AssessmentId
                             join s in _dbContext.Subjects on a.SubjectId equals s.SubjectId into subj
                             from sub in subj.DefaultIfEmpty()
                             where r.StudentId == studentId
                             select new { r.ObtainedMarks, r.TotalMarks, r.Percentage, r.Rank, r.ResultStatus, SubjectName = sub != null ? sub.SubjectName : "General" })
                             .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        
        // --- SUMMARY SHEET ---
        var wsSummary = workbook.Worksheets.Add("Executive Summary");
        wsSummary.Cell(2, 2).Value = "STUDENT PERFORMANCE INSIGHTS";
        wsSummary.Range(2, 2, 2, 5).Merge().Style.Font.SetBold().Font.FontSize = 20;
        wsSummary.Range(2, 2, 2, 5).Style.Font.FontColor = XLColor.DarkBlue;
        
        wsSummary.Cell(3, 2).Value = $"Name: {student.FullName} | USN: {student.EnrollmentNumber ?? "N/A"}";
        wsSummary.Range(3, 2, 3, 5).Merge().Style.Font.FontSize = 14;
        
        wsSummary.Cell(5, 2).Value = "Branch";
        wsSummary.Cell(5, 3).Value = student.Class?.Name ?? "N/A";
        wsSummary.Cell(6, 2).Value = "Batch";
        wsSummary.Cell(6, 3).Value = student.Batch?.Name ?? "N/A";
        
        var overallPercentage = results.Any() ? results.Average(r => r.Percentage) / 100 : 0;
        wsSummary.Cell(7, 2).Value = "Overall Percentage";
        wsSummary.Cell(7, 3).Value = overallPercentage;
        wsSummary.Cell(7, 3).Style.NumberFormat.Format = "0.0%";
        
        wsSummary.Range(5, 2, 7, 2).Style.Font.SetBold().Fill.BackgroundColor = XLColor.AliceBlue;
        wsSummary.Range(5, 2, 7, 3).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
        wsSummary.Columns().AdjustToContents();

        // --- DATA SHEET ---
        var ws = workbook.Worksheets.Add("Subject Breakdown");
        
        StyleExcelHeader(ws, 3, $"Subject Breakdown - {student.FullName}", $"Institution: {student.College?.CollegeName ?? "N/A"}");

        var headers = new[] { "Subject", "Average Marks", "Percentage" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(5, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold().Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        int row = 6;
        var groupedBySubject = results.GroupBy(r => r.SubjectName).ToList();
        
        if (!groupedBySubject.Any())
        {
            ws.Cell(row, 1).Value = "No sufficient assessment data available.";
        }
        else
        {
            foreach (var group in groupedBySubject)
            {
                ws.Cell(row, 1).Value = group.Key;
                ws.Cell(row, 2).Value = group.Average(x => x.ObtainedMarks);
                
                var pct = group.Average(x => x.Percentage) / 100;
                ws.Cell(row, 3).Value = pct;
                ws.Cell(row, 3).Style.NumberFormat.Format = "0.0%";
                
                if (row % 2 == 0) ws.Range(row, 1, row, 3).Style.Fill.BackgroundColor = XLColor.AliceBlue;
                ws.Range(row, 1, row, 3).Style.Border.SetBottomBorder(XLBorderStyleValues.Thin).Border.SetBottomBorderColor(XLColor.LightGray);
                row++;
            }
        }

        ws.Columns().AdjustToContents();
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
