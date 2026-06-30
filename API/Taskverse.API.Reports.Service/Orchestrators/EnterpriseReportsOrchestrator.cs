using System.IO.Compression;
using System.Text;
using Taskverse.API.Reports.Service.Managers;
using Taskverse.API.Reports.Service.Models;

namespace Taskverse.API.Reports.Service.Orchestrators;

public class EnterpriseReportsOrchestrator : IEnterpriseReportsOrchestrator
{
    private readonly IEnterpriseReportsManager _manager;

    public EnterpriseReportsOrchestrator(IEnterpriseReportsManager manager)
    {
        _manager = manager;
    }

    public async Task ExecuteRawSqlAsync(string sql, CancellationToken ct) =>
        await _manager.ExecuteRawSqlAsync(sql, ct);

    public async Task<List<FilterOptionResponse>> GetCollegesAsync(CancellationToken ct) =>
        await _manager.GetCollegesAsync(ct);

    public async Task<List<FilterOptionResponse>> GetBranchesAsync(Guid? collegeId, CancellationToken ct) =>
        await _manager.GetBranchesAsync(collegeId, ct);

    public async Task<List<FilterOptionResponse>> GetBatchesAsync(Guid? classId, CancellationToken ct) =>
        await _manager.GetBatchesAsync(classId, ct);

    public async Task<List<FilterOptionResponse>> GetTrainersAsync(Guid? collegeId, CancellationToken ct) =>
        await _manager.GetTrainersAsync(collegeId, ct);

    public async Task<CollegeWiseReportResponse> GetCollegeWiseReportAsync(
        Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear, CancellationToken ct)
    {
        var rows = await _manager.BuildCollegeWiseRowsAsync(collegeId, dateFrom, dateTo, academicYear, ct);
        return new CollegeWiseReportResponse
        {
            Metadata = BuildMetadata("College Wise Performance Report", collegeId, null, null, dateFrom, dateTo, academicYear),
            Summary = new CollegeWiseSummaryResponse
            {
                TotalColleges = rows.Count,
                TotalStudents = rows.Sum(r => r.TotalStudents),
                TotalTrainers = rows.Sum(r => r.TotalTrainers),
                TotalAssessments = rows.Sum(r => r.TotalAssessments),
                AverageScore = rows.Count > 0 ? Math.Round(rows.Average(r => r.AverageScore), 2) : 0m,
                OverallPassPercentage = rows.Count > 0 ? Math.Round(rows.Average(r => r.PassPercentage), 2) : 0m
            },
            Rows = rows
        };
    }

    public async Task<BranchWiseReportResponse> GetBranchWiseReportAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct)
    {
        var rows = await _manager.BuildBranchWiseRowsAsync(collegeId, classId, batchId, dateFrom, dateTo, ct);
        return new BranchWiseReportResponse
        {
            Metadata = BuildMetadata("Branch Wise Performance Report", collegeId, classId, batchId, dateFrom, dateTo, null),
            Summary = new BranchWiseSummaryResponse
            {
                TotalBranches = rows.Count,
                TotalStudents = rows.Sum(r => r.TotalStudents),
                TotalTrainers = rows.Sum(r => r.TotalTrainers),
                TotalAssessments = rows.Sum(r => r.TotalAssessments),
                AverageMarks = rows.Count > 0 ? Math.Round(rows.Average(r => r.AverageMarks), 2) : 0m,
                OverallPassPercentage = rows.Count > 0 ? Math.Round(rows.Average(r => r.PassPercentage), 2) : 0m
            },
            Rows = rows
        };
    }

    public async Task<StudentPerformanceReportResponse> GetStudentPerformanceReportAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel, CancellationToken ct)
    {
        var rows = await _manager.BuildStudentPerformanceRowsAsync(
            collegeId, classId, batchId, studentId, trainerId, assessmentId, dateFrom, dateTo, performanceLevel, ct);
        return new StudentPerformanceReportResponse
        {
            Metadata = BuildMetadata("Student Performance Report", collegeId, classId, batchId, dateFrom, dateTo, null),
            Summary = new StudentPerformanceSummaryResponse
            {
                TotalStudents = rows.Count,
                AveragePercentage = rows.Count > 0 ? Math.Round(rows.Average(r => r.OverallPercentage), 2) : 0m,
                PassPercentage = rows.Count > 0 ? Math.Round(100m * rows.Count(r => r.OverallPercentage >= 50) / rows.Count, 2) : 0m,
                HighestPercentage = rows.Count > 0 ? Math.Round(rows.Max(r => r.OverallPercentage), 2) : 0m,
                LowestPercentage = rows.Count > 0 ? Math.Round(rows.Min(r => r.OverallPercentage), 2) : 0m,
                PlacementReadyCount = rows.Count(r => r.OverallPercentage >= 70)
            },
            Rows = rows
        };
    }

    public async Task<byte[]> ExportCollegeWisePdfAsync(
        Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear, CancellationToken ct)
    {
        var rows = await _manager.BuildCollegeWiseRowsAsync(collegeId, dateFrom, dateTo, academicYear, ct);
        return BuildEnterprisePdf(
            "College Wise Performance Report",
            new[] { "College", "Students", "Trainers", "Assessments", "Completed", "Avg Score", "Highest", "Lowest", "Pass %", "Active", "Grade" },
            rows.Select(r => new[] {
                r.CollegeName, r.TotalStudents.ToString(), r.TotalTrainers.ToString(),
                r.TotalAssessments.ToString(), r.AssessmentsCompleted.ToString(),
                r.AverageScore.ToString("0.##"), r.HighestScore.ToString("0.##"),
                r.LowestScore.ToString("0.##"), r.PassPercentage.ToString("0.##") + "%",
                r.ActiveStudents.ToString(), r.PerformanceGrade
            }).ToList(),
            $"Total Colleges: {rows.Count} | Total Students: {rows.Sum(r => r.TotalStudents)} | Avg Score: {(rows.Count > 0 ? rows.Average(r => r.AverageScore) : 0):0.##}",
            collegeId, dateFrom, dateTo, academicYear
        );
    }

    public async Task<byte[]> ExportCollegeWiseExcelAsync(
        Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear, CancellationToken ct)
    {
        var rows = await _manager.BuildCollegeWiseRowsAsync(collegeId, dateFrom, dateTo, academicYear, ct);
        return BuildEnterpriseExcel(
            "College Wise Performance Report",
            new[] { "College Name", "Total Students", "Total Trainers", "Total Assessments", "Completed", "Avg Score", "Highest Score", "Lowest Score", "Pass %", "Active Students", "Grade" },
            rows.Select(r => new[] {
                r.CollegeName, r.TotalStudents.ToString(), r.TotalTrainers.ToString(),
                r.TotalAssessments.ToString(), r.AssessmentsCompleted.ToString(),
                r.AverageScore.ToString("0.##"), r.HighestScore.ToString("0.##"),
                r.LowestScore.ToString("0.##"), r.PassPercentage.ToString("0.##"),
                r.ActiveStudents.ToString(), r.PerformanceGrade
            }).ToList(),
            collegeId, dateFrom, dateTo, academicYear
        );
    }

    public async Task<byte[]> ExportBranchWisePdfAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct)
    {
        var rows = await _manager.BuildBranchWiseRowsAsync(collegeId, classId, batchId, dateFrom, dateTo, ct);
        return BuildEnterprisePdf(
            "Branch Wise Performance Report",
            new[] { "Branch", "Students", "Trainers", "Assessments", "Avg Marks", "Highest", "Lowest", "Pass %", "Strong Topics", "Weak Topics" },
            rows.Select(r => new[] {
                r.BranchName, r.TotalStudents.ToString(), r.TotalTrainers.ToString(),
                r.TotalAssessments.ToString(), r.AverageMarks.ToString("0.##"),
                r.HighestMarks.ToString("0.##"), r.LowestMarks.ToString("0.##"),
                r.PassPercentage.ToString("0.##") + "%",
                string.Join(", ", r.StrongestTopics.Take(3)),
                string.Join(", ", r.WeakestTopics.Take(3))
            }).ToList(),
            $"Total Branches: {rows.Count} | Total Students: {rows.Sum(r => r.TotalStudents)} | Avg Marks: {(rows.Count > 0 ? rows.Average(r => r.AverageMarks) : 0):0.##}",
            collegeId, dateFrom, dateTo, null
        );
    }

    public async Task<byte[]> ExportBranchWiseExcelAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct)
    {
        var rows = await _manager.BuildBranchWiseRowsAsync(collegeId, classId, batchId, dateFrom, dateTo, ct);
        return BuildEnterpriseExcel(
            "Branch Wise Performance Report",
            new[] { "Branch Name", "Total Students", "Total Trainers", "Total Assessments", "Avg Marks", "Highest Marks", "Lowest Marks", "Pass %", "Strongest Topics", "Weakest Topics" },
            rows.Select(r => new[] {
                r.BranchName, r.TotalStudents.ToString(), r.TotalTrainers.ToString(),
                r.TotalAssessments.ToString(), r.AverageMarks.ToString("0.##"),
                r.HighestMarks.ToString("0.##"), r.LowestMarks.ToString("0.##"),
                r.PassPercentage.ToString("0.##"),
                string.Join(", ", r.StrongestTopics),
                string.Join(", ", r.WeakestTopics)
            }).ToList(),
            collegeId, dateFrom, dateTo, null
        );
    }

    public async Task<byte[]> ExportStudentPerformancePdfAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel, CancellationToken ct)
    {
        var rows = await _manager.BuildStudentPerformanceRowsAsync(
            collegeId, classId, batchId, studentId, trainerId, assessmentId, dateFrom, dateTo, performanceLevel, ct);
        return BuildEnterprisePdf(
            "Student Performance Report",
            new[] { "Name", "USN", "Branch", "Batch", "Marks", "Total", "%", "Rank", "Readiness", "Grade" },
            rows.Select(s => new[] {
                s.Name, s.EnrollmentNumber, s.BranchName, s.BatchName,
                s.TotalObtained.ToString("0.##"), s.TotalMarks.ToString("0.##"),
                s.OverallPercentage.ToString("0.##") + "%", s.OverallRank.ToString(),
                s.OverallPercentage >= 70 ? "Ready" : "Needs Work", GetGrade(s.OverallPercentage)
            }).ToList(),
            $"Total Students: {rows.Count} | Avg: {(rows.Count > 0 ? rows.Average(s => s.OverallPercentage) : 0):0.##}% | Placement Ready: {rows.Count(s => s.OverallPercentage >= 70)}",
            collegeId, dateFrom, dateTo, null
        );
    }

    public async Task<byte[]> ExportStudentPerformanceExcelAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel, CancellationToken ct)
    {
        var rows = await _manager.BuildStudentPerformanceRowsAsync(
            collegeId, classId, batchId, studentId, trainerId, assessmentId, dateFrom, dateTo, performanceLevel, ct);
        return BuildEnterpriseExcel(
            "Student Performance Report",
            new[] { "Name", "USN/Roll", "College", "Branch", "Batch", "Obtained Marks", "Total Marks", "Percentage", "Rank", "Placement Readiness", "Grade", "Strong Topics", "Weak Topics", "Priority" },
            rows.Select(s => new[] {
                s.Name, s.EnrollmentNumber, s.CollegeName, s.BranchName, s.BatchName,
                s.TotalObtained.ToString("0.##"), s.TotalMarks.ToString("0.##"),
                s.OverallPercentage.ToString("0.##"),
                s.OverallRank.ToString(),
                s.OverallPercentage >= 70 ? "Ready" : "Needs Improvement",
                GetGrade(s.OverallPercentage),
                string.Join(", ", s.AiInsights.StrongTopics.Take(3)),
                string.Join(", ", s.AiInsights.WeakTopics.Take(3)),
                s.AiInsights.PriorityLevel
            }).ToList(),
            collegeId, dateFrom, dateTo, null
        );
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  PRIVATE PDF & EXCEL BUILDERS (IDENTICAL TO PREVIOUS FOR EXCELLENCE)
    // ═══════════════════════════════════════════════════════════════════════

    private static ReportMetadataResponse BuildMetadata(
        string title, Guid? collegeId, Guid? classId, Guid? batchId,
        DateTime? dateFrom, DateTime? dateTo, string? academicYear)
    {
        var filters = new Dictionary<string, string>();
        if (collegeId.HasValue) filters["College"] = collegeId.Value.ToString();
        if (classId.HasValue) filters["Branch"] = classId.Value.ToString();
        if (batchId.HasValue) filters["Batch"] = batchId.Value.ToString();
        if (dateFrom.HasValue) filters["From"] = dateFrom.Value.ToString("yyyy-MM-dd");
        if (dateTo.HasValue) filters["To"] = dateTo.Value.ToString("yyyy-MM-dd");
        if (!string.IsNullOrEmpty(academicYear)) filters["Academic Year"] = academicYear;

        return new ReportMetadataResponse
        {
            ReportTitle = title,
            GeneratedDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            GeneratedTime = DateTime.UtcNow.ToString("HH:mm:ss"),
            GeneratedBy = "TASKVERSE System",
            AppliedFilters = filters,
            AcademicYear = academicYear ?? DateTime.UtcNow.Year.ToString()
        };
    }

    private static string GetGrade(decimal percentage) =>
        percentage >= 90 ? "A+" :
        percentage >= 80 ? "A" :
        percentage >= 70 ? "B+" :
        percentage >= 60 ? "B" :
        percentage >= 50 ? "C" :
        percentage >= 40 ? "D" : "F";

    private static byte[] BuildEnterprisePdf(
        string title, string[] headers, List<string[]> dataRows,
        string summaryText, Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear)
    {
        var lines = new List<string>
        {
            "═══════════════════════════════════════════════════════════════════",
            "                         TASKVERSE",
            "              Assessment & Analytics Platform",
            "═══════════════════════════════════════════════════════════════════",
            "",
            $"  Report: {title}",
            $"  Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            $"  Filters: College={collegeId?.ToString() ?? "All"}, From={dateFrom?.ToString("yyyy-MM-dd") ?? "N/A"}, To={dateTo?.ToString("yyyy-MM-dd") ?? "N/A"}, Year={academicYear ?? "All"}",
            "",
            "───────────────────────────────────────────────────────────────────",
            $"  SUMMARY: {summaryText}",
            "───────────────────────────────────────────────────────────────────",
            "",
            string.Join(" | ", headers)
        };

        foreach (var row in dataRows)
            lines.Add(string.Join(" | ", row));

        if (dataRows.Count == 0)
            lines.Add("No data found for the selected filters.");

        lines.Add("");
        lines.Add("───────────────────────────────────────────────────────────────────");
        lines.Add($"  Total Records: {dataRows.Count}");
        lines.Add($"  Page 1 of 1");
        lines.Add("  Generated by TASKVERSE — Assessment & Analytics Platform");
        lines.Add("═══════════════════════════════════════════════════════════════════");

        var content = new StringBuilder();
        var yPosition = 760;
        var fontSize = dataRows.Count > 20 ? 7 : 9;
        foreach (var line in lines)
        {
            content.AppendLine($"BT /F1 {fontSize} Tf 30 {yPosition} Td ({EscapePdfText(line)}) Tj ET");
            yPosition -= (fontSize + 3);
            if (yPosition < 40) yPosition = 760;
        }

        var contentBytes = Encoding.ASCII.GetBytes(content.ToString());
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {contentBytes.Length} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>"
        };

        var pdf = new StringBuilder();
        pdf.AppendLine("%PDF-1.4");
        var offsets = new List<int>();
        var currentOffset = 0;
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(currentOffset);
            pdf.Append(index + 1).Append(" 0 obj\n");
            pdf.Append(objects[index]).Append("\nendobj\n");
            currentOffset = pdf.Length;
        }

        var xrefOffset = pdf.Length;
        pdf.AppendLine("xref");
        pdf.AppendLine($"0 {objects.Count + 1}");
        pdf.AppendLine("0000000000 65535 f ");
        foreach (var offset in offsets)
            pdf.Append(offset.ToString("D10")).AppendLine(" 00000 n ");

        pdf.AppendLine("trailer");
        pdf.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        pdf.AppendLine($"startxref\n{xrefOffset}\n%%EOF");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static byte[] BuildEnterpriseExcel(
        string title, string[] headers, List<string[]> dataRows,
        Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear)
    {
        var sheetXml = new StringBuilder();
        sheetXml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sheetXml.AppendLine("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");

        sheetXml.AppendLine("<cols>");
        for (var col = 0; col < headers.Length; col++)
            sheetXml.AppendLine($"<col min=\"{col + 1}\" max=\"{col + 1}\" width=\"18\" customWidth=\"1\"/>");
        sheetXml.AppendLine("</cols>");

        sheetXml.AppendLine("<sheetData>");

        AppendStyledRow(sheetXml, new[] { title }, 0);
        AppendStyledRow(sheetXml, new[] { $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC | Filters: College={collegeId?.ToString() ?? "All"}, From={dateFrom?.ToString("yyyy-MM-dd") ?? "N/A"}, To={dateTo?.ToString("yyyy-MM-dd") ?? "N/A"}" }, 0);
        AppendStyledRow(sheetXml, new[] { "" }, 0);
        AppendStyledRow(sheetXml, headers, 1);

        foreach (var row in dataRows)
            AppendStyledRow(sheetXml, row, 2);

        if (dataRows.Count == 0)
            AppendStyledRow(sheetXml, new[] { "No data found for the selected filters." }, 0);

        AppendStyledRow(sheetXml, new[] { $"Total Records: {dataRows.Count}" }, 1);

        sheetXml.AppendLine("</sheetData>");

        if (headers.Length > 0)
        {
            var lastCol = (char)('A' + Math.Min(headers.Length - 1, 25));
            sheetXml.AppendLine($"<autoFilter ref=\"A4:{lastCol}{4 + dataRows.Count}\"/>");
        }

        sheetXml.AppendLine("<sheetViews><sheetView tabSelected=\"1\" workbookViewId=\"0\">");
        sheetXml.AppendLine("<pane ySplit=\"4\" topLeftCell=\"A5\" activePane=\"bottomLeft\" state=\"frozen\"/>");
        sheetXml.AppendLine("</sheetView></sheetViews>");

        sheetXml.AppendLine("</worksheet>");

        var package = new MemoryStream();
        using (var archive = new ZipArchive(package, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", BuildContentTypesXml());
            WriteEntry(archive, "_rels/.rels", BuildRelsXml());
            WriteEntry(archive, "docProps/app.xml", BuildAppXml());
            WriteEntry(archive, "docProps/core.xml", BuildCoreXml());
            WriteEntry(archive, "xl/workbook.xml", BuildWorkbookXml());
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelsXml());
            WriteEntry(archive, "xl/worksheets/sheet1.xml", sheetXml.ToString());
            WriteEntry(archive, "xl/styles.xml", BuildEnterpriseStylesXml());
        }
        package.Position = 0;
        return package.ToArray();
    }

    private static void AppendStyledRow(StringBuilder builder, IReadOnlyList<string> cells, int styleIndex)
    {
        builder.AppendLine("<row>");
        for (var index = 0; index < cells.Count; index++)
        {
            var value = EscapeXml(cells[index]);
            builder.AppendLine($"<c t=\"inlineStr\" s=\"{styleIndex}\"><is><t>{value}</t></is></c>");
        }
        builder.AppendLine("</row>");
    }

    private static string BuildEnterpriseStylesXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
        "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
        "<fonts count=\"3\">" +
        "<font><sz val=\"11\"/><name val=\"Calibri\"/></font>" +
        "<font><b/><sz val=\"11\"/><color rgb=\"FFFFFFFF\"/><name val=\"Calibri\"/></font>" +
        "<font><sz val=\"10\"/><name val=\"Calibri\"/></font>" +
        "</fonts>" +
        "<fills count=\"4\">" +
        "<fill><patternFill patternType=\"none\"/></fill>" +
        "<fill><patternFill patternType=\"gray125\"/></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FF1E3A5F\"/></patternFill></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFF0F4F8\"/></patternFill></fill>" +
        "</fills>" +
        "<borders count=\"2\">" +
        "<border/>" +
        "<border><left style=\"thin\"><color auto=\"1\"/></left><right style=\"thin\"><color auto=\"1\"/></right><top style=\"thin\"><color auto=\"1\"/></top><bottom style=\"thin\"><color auto=\"1\"/></bottom></border>" +
        "</borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"3\">" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>" +
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>" +
        "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"1\" applyFont=\"1\" applyBorder=\"1\"/>" +
        "</cellXfs>" +
        "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
        "</styleSheet>";

    private static string BuildContentTypesXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/><Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\"/><Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/></Types>";

    private static string BuildRelsXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>";

    private static string BuildWorkbookXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Results\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>";

    private static string BuildWorkbookRelsXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>";

    private static string BuildAppXml() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\"><Application>Taskverse</Application></Properties>";

    private static string BuildCoreXml() =>
        $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><dc:title>Taskverse Result Report</dc:title><dc:creator>Taskverse</dc:creator><cp:revision>1</cp:revision><dcterms:created xsi:type=\"dcterms:W3CDTF\">{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</dcterms:created></cp:coreProperties>";

    private static void WriteEntry(ZipArchive archive, string entryName, string content)
    {
        var entry = archive.CreateEntry(entryName);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string EscapePdfText(string value) => value
        .Replace("\\", "\\\\")
        .Replace("(", "\\(")
        .Replace(")", "\\)");

    private static string EscapeXml(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");
}
