using Taskverse.API.Reports.Service.Models;

namespace Taskverse.API.Reports.Service.Managers;

public interface IEnterpriseReportsManager
{
    Task ExecuteRawSqlAsync(string sql, CancellationToken ct);
    Task<List<FilterOptionResponse>> GetCollegesAsync(CancellationToken ct);
    Task<List<FilterOptionResponse>> GetBranchesAsync(Guid? collegeId, CancellationToken ct);
    Task<List<FilterOptionResponse>> GetBatchesAsync(Guid? classId, CancellationToken ct);
    Task<List<FilterOptionResponse>> GetTrainersAsync(Guid? collegeId, CancellationToken ct);

    Task<List<CollegeWiseRowResponse>> BuildCollegeWiseRowsAsync(
        Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear, CancellationToken ct);

    Task<List<BranchWiseRowResponse>> BuildBranchWiseRowsAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct);

    Task<List<StudentPerformanceRowResponse>> BuildStudentPerformanceRowsAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel, CancellationToken ct);
}
