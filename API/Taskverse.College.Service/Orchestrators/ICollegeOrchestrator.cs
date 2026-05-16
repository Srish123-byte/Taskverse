using Taskverse.API.College.Service.DTOs;

namespace Taskverse.API.College.Service.Orchestrators;

public interface ICollegeOrchestrator
{
    Task<List<PendingUserDto>> GetPendingUsersByCollege(Guid collegeId);
    Task<List<PendingUserDto>> GetPendingUsersForCollegeAdmin(Guid collegeAdminUserId);
    Task<CollegeClassSummaryDto> CreateClass(Guid collegeId, CreateCollegeClassDto dto);
    Task<CollegeBatchSummaryDto> CreateBatch(Guid collegeId, Guid classId, CreateCollegeBatchDto dto);
    Task ApproveUser(Guid collegeId, string userId, CollegeUserActionDto dto);
    Task RejectUser(Guid collegeId, string userId, CollegeUserActionDto dto);
}
