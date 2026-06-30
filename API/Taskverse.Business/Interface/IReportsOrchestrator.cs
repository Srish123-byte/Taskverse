using Taskverse.Business.DTOs;

namespace Taskverse.Business.Interface;

public interface IReportsOrchestrator
{
    Task<ReportDto> GenerateReport(GenerateReportDto dto);
    Task<ReportDto> GetReport(string reportId);
    Task<UserPerformanceReportDto> GetUserPerformanceReport(string userId);
    Task<AssessmentReportDto> GetAssessmentReport(string assessmentId);
    Task<List<ReportDto>> GetReportsByUser(string userId);
    Task<List<StudentResultDto>> GetStudentResults(Guid studentId);
    Task<StudentResultDto> GetStudentAttemptResult(Guid attemptId);

    // Enterprise Reports
    Task<CollegeWiseReportDto> GetCollegeWiseReport(Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear);
    Task<byte[]> ExportCollegeWisePdf(Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear);
    Task<byte[]> ExportCollegeWiseExcel(Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear);

    Task<BranchWiseReportDto> GetBranchWiseReport(Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo);
    Task<byte[]> ExportBranchWisePdf(Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo);
    Task<byte[]> ExportBranchWiseExcel(Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo);

    Task<StudentPerformanceReportDto> GetStudentPerformanceReport(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel);

    Task<byte[]> ExportStudentPerformancePdf(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel);

    Task<byte[]> ExportStudentPerformanceExcel(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel);

    Task<List<FilterOptionDto>> GetCollegesFilter();
    Task<List<FilterOptionDto>> GetBranchesFilter(Guid? collegeId);
    Task<List<FilterOptionDto>> GetBatchesFilter(Guid? classId);
    Task<List<FilterOptionDto>> GetTrainersFilter(Guid? collegeId);
}
