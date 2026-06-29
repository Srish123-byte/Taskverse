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

        var totalStudents = await _dbContext.Students.CountAsync(s => s.CollegeId == collegeId, cancellationToken);
        var totalBranches = await _dbContext.Classes.CountAsync(c => c.CollegeId == collegeId, cancellationToken);
        var totalTrainers = await _dbContext.Trainers.CountAsync(t => t.CollegeId == collegeId, cancellationToken);

        var results = await (from r in _dbContext.Results
                             join s in _dbContext.Students on r.StudentId equals s.StudentId
                             where s.CollegeId == collegeId
                             select r).ToListAsync(cancellationToken);

        var averagePercentage = results.Any() ? results.Average(r => r.Percentage) : 0;
        var passPercentage = results.Any() ? (decimal)results.Count(r => r.ResultStatus == Taskverse.Data.Enums.ResultStatus.Pass) / results.Count * 100 : 0;

        var studentStats = await (from s in _dbContext.Students
                                  where s.CollegeId == collegeId
                                  join c in _dbContext.Classes on s.ClassId equals c.ClassId into cl
                                  from classObj in cl.DefaultIfEmpty()
                                  select new
                                  {
                                      USN = s.EnrollmentNumber ?? "N/A",
                                      Name = s.FullName,
                                      Branch = classObj != null ? classObj.Name : "N/A",
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

                BuildPdfHeader(page, "College Wise Performance Report", college.CollegeName);

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Spacing(20);
                    
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
                                c.Item().AlignCenter().Text(title).FontSize(9).FontColor(Colors.Grey.Darken2);
                                c.Item().AlignCenter().Text(value).FontSize(14).SemiBold();
                            });
                        }

                        AddCard("Total Students", totalStudents.ToString());
                        AddCard("Total Branches", totalBranches.ToString());
                        AddCard("Average %", $"{averagePercentage:F1}%");
                        AddCard("Pass %", $"{passPercentage:F1}%");
                    });

                    // Data Table
                    col.Item().PaddingBottom(5).Text("Student Performance Roster").FontSize(14).SemiBold();
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
                            table.Cell().Background(bg).Padding(5).Text(status).FontColor(status == "Pass" ? Colors.Green.Darken2 : Colors.Red.Darken2);
                            
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

        var studentStats = await (from s in _dbContext.Students
                                  where s.CollegeId == collegeId
                                  join c in _dbContext.Classes on s.ClassId equals c.ClassId into cl
                                  from classObj in cl.DefaultIfEmpty()
                                  select new { s.StudentId, s.EnrollmentNumber, s.FullName, BranchName = classObj != null ? classObj.Name : "N/A" })
                                  .ToListAsync(cancellationToken);

        var results = await (from r in _dbContext.Results
                             join s in _dbContext.Students on r.StudentId equals s.StudentId
                             where s.CollegeId == collegeId
                             select r).ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("College Report");
        
        StyleExcelHeader(ws, 7, "College Wise Performance Report", $"Institution: {college.CollegeName}");

        var headers = new[] { "USN", "Student Name", "Branch", "Assessment Count", "Average Marks", "Percentage", "Status" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(5, i + 1).Value = headers[i];
            ws.Cell(5, i + 1).Style.Font.SetBold().Fill.BackgroundColor = XLColor.LightGray;
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
            ws.Cell(row, 6).Value = stuResults.Any() ? stuResults.Average(r => r.Percentage) / 100 : 0;
            ws.Cell(row, 6).Style.NumberFormat.Format = "0.0%";
            ws.Cell(row, 7).Value = stuResults.OrderByDescending(r => r.GeneratedAt).Select(r => r.ResultStatus.ToString()).FirstOrDefault() ?? "N/A";
            
            if (row % 2 == 0) ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.AliceBlue;
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

        var students = await _dbContext.Students.Where(s => s.ClassId == branchId).ToListAsync(cancellationToken);
        var studentIds = students.Select(s => s.StudentId).ToList();
        var results = await _dbContext.Results.Where(r => studentIds.Contains(r.StudentId)).ToListAsync(cancellationToken);

        var totalStudents = students.Count;
        var averagePercentage = results.Any() ? results.Average(r => r.Percentage) : 0;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                BuildPdfHeader(page, "Branch Wise Performance Report", college?.CollegeName ?? "N/A");

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Spacing(20);
                    
                    col.Item().Text($"Branch: {branch.Name}").FontSize(16).SemiBold();
                    
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); });
                        table.Cell().Padding(5).Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c => { c.Item().AlignCenter().Text("Total Students"); c.Item().AlignCenter().Text(totalStudents.ToString()).FontSize(14).SemiBold(); });
                        table.Cell().Padding(5).Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2).Column(c => { c.Item().AlignCenter().Text("Average %"); c.Item().AlignCenter().Text($"{averagePercentage:F1}%").FontSize(14).SemiBold(); });
                    });

                    col.Item().PaddingBottom(5).Text("Student Performance Table").FontSize(14).SemiBold();
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
                            table.Cell().Background(bg).Padding(5).Text(status).FontColor(status == "Pass" ? Colors.Green.Darken2 : Colors.Red.Darken2);
                            
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

        var students = await _dbContext.Students.Where(s => s.ClassId == branchId).ToListAsync(cancellationToken);
        var studentIds = students.Select(s => s.StudentId).ToList();
        var results = await _dbContext.Results.Where(r => studentIds.Contains(r.StudentId)).ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Branch Report");
        
        StyleExcelHeader(ws, 6, $"Branch Wise Performance Report - {branch.Name}", $"Institution: {college?.CollegeName ?? "N/A"}");

        var headers = new[] { "USN", "Student Name", "Average Marks", "Percentage", "Rank", "Status" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(5, i + 1).Value = headers[i];
            ws.Cell(5, i + 1).Style.Font.SetBold().Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 6;
        foreach (var stat in students)
        {
            var stuResults = results.Where(r => r.StudentId == stat.StudentId).ToList();
            ws.Cell(row, 1).Value = stat.EnrollmentNumber ?? "N/A";
            ws.Cell(row, 2).Value = stat.FullName;
            ws.Cell(row, 3).Value = stuResults.Any() ? stuResults.Average(r => r.ObtainedMarks) : 0;
            ws.Cell(row, 4).Value = stuResults.Any() ? stuResults.Average(r => r.Percentage) / 100 : 0;
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
            ws.Cell(row, 5).Value = stuResults.OrderByDescending(r => r.GeneratedAt).Select(r => r.Rank).FirstOrDefault();
            ws.Cell(row, 6).Value = stuResults.OrderByDescending(r => r.GeneratedAt).Select(r => r.ResultStatus.ToString()).FirstOrDefault() ?? "N/A";
            
            if (row % 2 == 0) ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.AliceBlue;
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

                BuildPdfHeader(page, "Individual Student Report", student.College?.CollegeName ?? "N/A");

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(col =>
                {
                    col.Spacing(15);
                    
                    // Student Info Section
                    col.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().PaddingBottom(5).Text("Student Information").SemiBold().FontSize(12);
                            c.Item().Text($"Name: {student.FullName}");
                            c.Item().Text($"USN: {student.EnrollmentNumber ?? "N/A"}");
                            c.Item().Text($"Branch: {student.Class?.Name ?? "N/A"}");
                            c.Item().Text($"Batch: {student.Batch?.Name ?? "N/A"}");
                        });
                    });

                    // Overall Performance
                    col.Item().Text("Overall Performance").SemiBold().FontSize(14);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); columns.RelativeColumn(); });
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(c => { c.Item().Text("Total Marks").FontColor(Colors.Grey.Medium); c.Item().Text($"{obtainedMarks:F1} / {totalMarks:F1}").SemiBold(); });
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(c => { c.Item().Text("Percentage").FontColor(Colors.Grey.Medium); c.Item().Text($"{overallPercentage:F1}%").SemiBold(); });
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(c => { c.Item().Text("Grade").FontColor(Colors.Grey.Medium); c.Item().Text(grade).SemiBold(); });
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(c => { c.Item().Text("Best Rank").FontColor(Colors.Grey.Medium); c.Item().Text(rank.ToString()).SemiBold(); });
                    });

                    // Assessment Breakdown
                    col.Item().PaddingTop(10).Text("Assessment Breakdown").SemiBold().FontSize(14);
                    
                    var groupedBySubject = results.GroupBy(r => r.SubjectName).ToList();
                    
                    if (!groupedBySubject.Any())
                    {
                        col.Item().Text("No sufficient assessment data available.").Italic().FontColor(Colors.Grey.Medium);
                    }
                    else
                    {
                        foreach(var group in groupedBySubject)
                        {
                            col.Item().PaddingLeft(10).Column(c =>
                            {
                                c.Item().Text(group.Key).SemiBold().FontSize(12).FontColor(Colors.Blue.Darken2);
                                c.Item().Text($"Average Marks: {group.Average(x => x.ObtainedMarks):F1} / {group.Average(x => x.TotalMarks):F1}");
                                c.Item().PaddingBottom(5).Text($"Percentage: {group.Average(x => x.Percentage):F1}%");
                            });
                        }
                        
                        col.Item().PaddingTop(10).Text("Performance Summary").SemiBold().FontSize(14);
                        var bestSubject = groupedBySubject.OrderByDescending(g => g.Average(x => x.Percentage)).FirstOrDefault()?.Key ?? "N/A";
                        var worstSubject = groupedBySubject.OrderBy(g => g.Average(x => x.Percentage)).FirstOrDefault()?.Key ?? "N/A";
                        
                        col.Item().Text($"Strengths: Excellent performance in {bestSubject}.");
                        col.Item().Text($"Weaknesses: Needs improvement in {worstSubject}.");
                        col.Item().Text($"Trainer Remarks: Consistent participation and effort observed.");
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
        var ws = workbook.Worksheets.Add("Student Report");
        
        StyleExcelHeader(ws, 4, $"Individual Student Report - {student.FullName}", $"Institution: {student.College?.CollegeName ?? "N/A"}");

        ws.Cell(6, 1).Value = "Student Information";
        ws.Range(6, 1, 6, 4).Merge().Style.Font.SetBold().Fill.BackgroundColor = XLColor.LightGray;
        ws.Cell(7, 1).Value = "Name"; ws.Cell(7, 2).Value = student.FullName;
        ws.Cell(8, 1).Value = "USN"; ws.Cell(8, 2).Value = student.EnrollmentNumber ?? "N/A";
        ws.Cell(9, 1).Value = "Branch"; ws.Cell(9, 2).Value = student.Class?.Name ?? "N/A";
        ws.Cell(10, 1).Value = "Batch"; ws.Cell(10, 2).Value = student.Batch?.Name ?? "N/A";

        ws.Cell(12, 1).Value = "Assessment Breakdown";
        ws.Range(12, 1, 12, 4).Merge().Style.Font.SetBold().Fill.BackgroundColor = XLColor.LightGray;
        
        ws.Cell(13, 1).Value = "Subject";
        ws.Cell(13, 2).Value = "Average Marks";
        ws.Cell(13, 3).Value = "Percentage";
        ws.Range(13, 1, 13, 3).Style.Font.SetBold();

        int row = 14;
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
                ws.Cell(row, 3).Value = group.Average(x => x.Percentage) / 100;
                ws.Cell(row, 3).Style.NumberFormat.Format = "0.0%";
                row++;
            }
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
