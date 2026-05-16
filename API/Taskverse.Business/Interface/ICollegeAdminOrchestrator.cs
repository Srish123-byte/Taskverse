using Taskverse.Business.DTOs;

namespace Taskverse.Business.Interface;

public interface ICollegeAdminOrchestrator
{
    Task<CollegeAdminDashboardDto> GetDashboard(Guid collegeId);
    Task<ClassConfigurationDto> GetClassConfiguration(Guid collegeId);
    Task<List<PendingUserDto>> GetPendingUsersForCollegeAdmin(Guid collegeAdminUserId);
    Task<CollegeClassSummaryDto> CreateClass(Guid collegeId, CreateCollegeClassDto dto);
    Task<CollegeBatchSummaryDto> CreateBatch(Guid collegeId, string classId, CreateCollegeBatchDto dto);
    Task ApproveUser(Guid collegeId, string userId, UserActionDto dto);
    Task RejectUser(Guid collegeId, string userId, UserActionDto dto);
}
