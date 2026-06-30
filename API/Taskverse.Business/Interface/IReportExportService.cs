namespace Taskverse.Business.Interface;

public interface IReportExportService
{
    Task<ReportExportFile> GenerateCollegeReportAsync(Guid collegeId, string format, CancellationToken cancellationToken = default);
    Task<ReportExportFile> GenerateBranchReportAsync(Guid classId, string format, CancellationToken cancellationToken = default);
    Task<ReportExportFile> GenerateStudentReportAsync(Guid studentId, string format, CancellationToken cancellationToken = default);
}

public sealed record ReportExportFile(
    string FileName,
    string ContentType,
    byte[] Content);
