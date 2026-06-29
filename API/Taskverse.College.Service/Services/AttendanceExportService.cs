using System.Globalization;
using System.Security;
using System.Text;
using Taskverse.API.College.Service.DTOs;

namespace Taskverse.API.College.Service.Services;

public class AttendanceExportService : IAttendanceExportService
{
    private const string ContentType = "application/vnd.ms-excel";

    public AttendanceExportArtifact BuildWorkbook(
        string batchName,
        DateTime fromDate,
        DateTime toDate,
        IReadOnlyCollection<AttendanceHistoryItemDto> summaryItems,
        IReadOnlyCollection<AttendanceExportEntryDto> entryItems)
    {
        var safeBatchName = string.IsNullOrWhiteSpace(batchName) ? "batch" : batchName.Trim().Replace(' ', '-');
        var fileName = $"attendance-entries-{safeBatchName}-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.xls";
        var xml = BuildSpreadsheetXml(batchName, fromDate, toDate, summaryItems, entryItems);
        return new AttendanceExportArtifact(fileName, ContentType, Encoding.UTF8.GetBytes(xml));
    }

    private static string BuildSpreadsheetXml(
        string batchName,
        DateTime fromDate,
        DateTime toDate,
        IReadOnlyCollection<AttendanceHistoryItemDto> summaryItems,
        IReadOnlyCollection<AttendanceExportEntryDto> entryItems)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<?xml version=\"1.0\"?>");
        builder.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        builder.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
        builder.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
        builder.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
        builder.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");

        builder.AppendLine("  <Worksheet ss:Name=\"Entries\">");
        builder.AppendLine("    <Table>");
        AppendStringRow(builder, $"Attendance Entries - {batchName}");
        AppendStringRow(builder, $"Date Range: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
        builder.AppendLine("      <Row />");
        AppendHeaderRow(builder, "Date", "Session", "Student Name", "Enrollment Number", "Email", "Status", "Submitted By", "Batch Owner", "Submitted At", "Last Modified");

        foreach (var item in entryItems.OrderBy(row => row.AttendanceDate).ThenBy(row => (int)row.AttendanceSession).ThenBy(row => row.StudentName))
        {
            builder.AppendLine("      <Row>");
            AppendCell(builder, item.AttendanceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            AppendCell(builder, item.AttendanceSession.ToString());
            AppendCell(builder, item.StudentName);
            AppendCell(builder, item.EnrollmentNumber ?? "-");
            AppendCell(builder, item.Email);
            AppendCell(builder, item.AttendanceEntry.ToString());
            AppendCell(builder, item.SubmittedByTrainerName);
            AppendCell(builder, item.BatchOwnerTrainerName ?? "-");
            AppendCell(builder, item.SubmittedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            AppendCell(builder, item.LastModifiedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            builder.AppendLine("      </Row>");
        }

        builder.AppendLine("    </Table>");
        builder.AppendLine("  </Worksheet>");

        builder.AppendLine("  <Worksheet ss:Name=\"Summary\">");
        builder.AppendLine("    <Table>");
        AppendStringRow(builder, $"Attendance Summary - {batchName}");
        AppendStringRow(builder, $"Date Range: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
        builder.AppendLine("      <Row />");
        AppendHeaderRow(builder, "Date", "Session", "Submitted By", "Batch Owner", "Submitted At", "Last Modified", "Total Students", "Present", "Absent", "Attendance %");

        foreach (var item in summaryItems.OrderBy(row => row.AttendanceDate).ThenBy(row => (int)row.AttendanceSession))
        {
            builder.AppendLine("      <Row>");
            AppendCell(builder, item.AttendanceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            AppendCell(builder, item.AttendanceSession.ToString());
            AppendCell(builder, item.SubmittedByTrainerName);
            AppendCell(builder, item.BatchOwnerTrainerName ?? "-");
            AppendCell(builder, item.SubmittedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            AppendCell(builder, item.LastModifiedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            AppendNumberCell(builder, item.TotalStudents);
            AppendNumberCell(builder, item.PresentCount);
            AppendNumberCell(builder, item.AbsentCount);
            AppendCell(builder, item.AttendancePercentage.ToString("0.##", CultureInfo.InvariantCulture));
            builder.AppendLine("      </Row>");
        }

        builder.AppendLine("    </Table>");
        builder.AppendLine("  </Worksheet>");
        builder.AppendLine("</Workbook>");
        return builder.ToString();
    }

    private static void AppendHeaderRow(StringBuilder builder, params string[] values)
    {
        builder.AppendLine("      <Row>");
        foreach (var value in values)
        {
            AppendCell(builder, value);
        }
        builder.AppendLine("      </Row>");
    }

    private static void AppendStringRow(StringBuilder builder, string value)
    {
        builder.AppendLine("      <Row>");
        AppendCell(builder, value);
        builder.AppendLine("      </Row>");
    }

    private static void AppendCell(StringBuilder builder, string value)
    {
        builder.Append("        <Cell><Data ss:Type=\"String\">");
        builder.Append(SecurityElement.Escape(value) ?? string.Empty);
        builder.AppendLine("</Data></Cell>");
    }

    private static void AppendNumberCell(StringBuilder builder, int value)
    {
        builder.Append("        <Cell><Data ss:Type=\"Number\">");
        builder.Append(value.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("</Data></Cell>");
    }
}
