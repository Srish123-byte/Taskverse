using System;
using System.Threading;
using System.Threading.Tasks;

namespace Taskverse.API.Reports.Service.Managers;

public interface IReportExportManager
{
    Task<byte[]> GenerateCollegeWisePdfAsync(Guid collegeId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateCollegeWiseExcelAsync(Guid collegeId, CancellationToken cancellationToken = default);
    
    Task<byte[]> GenerateBranchWisePdfAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateBranchWiseExcelAsync(Guid branchId, CancellationToken cancellationToken = default);
    
    Task<byte[]> GenerateStudentPdfAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateStudentExcelAsync(Guid studentId, CancellationToken cancellationToken = default);
}
