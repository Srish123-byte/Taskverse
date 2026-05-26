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
}
