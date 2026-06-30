using Taskverse.API.Reports.Service.Models;

namespace Taskverse.API.Reports.Service.Orchestrators;

public interface IEnterpriseReportsOrchestrator
{
    Task ExecuteRawSqlAsync(string sql, CancellationToken ct);
    Task<List<FilterOptionResponse>> GetCollegesAsync(CancellationToken ct);
    Task<List<FilterOptionResponse>> GetBranchesAsync(Guid? collegeId, CancellationToken ct);
    Task<List<FilterOptionResponse>> GetBatchesAsync(Guid? classId, CancellationToken ct);
    Task<List<FilterOptionResponse>> GetTrainersAsync(Guid? collegeId, CancellationToken ct);

    Task<CollegeWiseReportResponse> GetCollegeWiseReportAsync(
        Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear, CancellationToken ct);

    Task<BranchWiseReportResponse> GetBranchWiseReportAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct);

    Task<StudentPerformanceReportResponse> GetStudentPerformanceReportAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel, CancellationToken ct);

    Task<byte[]> ExportCollegeWisePdfAsync(
        Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear, CancellationToken ct);

    Task<byte[]> ExportCollegeWiseExcelAsync(
        Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear, CancellationToken ct);

    Task<byte[]> ExportBranchWisePdfAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct);

    Task<byte[]> ExportBranchWiseExcelAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct);

    Task<byte[]> ExportStudentPerformancePdfAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel, CancellationToken ct);

    Task<byte[]> ExportStudentPerformanceExcelAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel, CancellationToken ct);
}
