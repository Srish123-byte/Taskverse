using Taskverse.API.College.Service.DTOs;

namespace Taskverse.API.College.Service.Services;

public interface IAttendanceExportService
{
    AttendanceExportArtifact BuildWorkbook(
        string batchName,
        DateTime fromDate,
        DateTime toDate,
        IReadOnlyCollection<AttendanceHistoryItemDto> summaryItems,
        IReadOnlyCollection<AttendanceExportEntryDto> entryItems);
}

public sealed record AttendanceExportArtifact(
    string FileName,
    string ContentType,
    byte[] Content);
