using Taskverse.Business.DTOs;

namespace Taskverse.Business.Interface;

public interface ICollegeAdminOrchestrator
{
    Task<CollegeAdminDashboardDto> GetDashboard(Guid collegeId);
    Task<ClassConfigurationDto> GetClassConfiguration(Guid collegeId);
    Task<List<PendingUserDto>> GetPendingUsers(Guid collegeId);
    Task<List<PendingUserDto>> GetPendingUsersForCollegeAdmin(Guid collegeAdminUserId);
    Task<List<ApprovedTrainerDto>> GetApprovedTrainers(Guid collegeId);
    Task<List<SubjectOptionDto>> GetSubjects();
    Task<CollegeClassSummaryDto> CreateClass(Guid collegeId, CreateCollegeClassDto dto);
    Task<CollegeClassSummaryDto> UpdateClass(Guid collegeId, string classId, UpdateCollegeClassDto dto);
    Task<CollegeBatchSummaryDto> CreateBatch(Guid collegeId, string classId, CreateCollegeBatchDto dto);
    Task<CollegeBatchSummaryDto> UpdateBatch(Guid collegeId, string classId, string batchId, UpdateCollegeBatchDto dto);
    Task<CollegeBatchSummaryDto> AssignBatchTrainers(Guid collegeId, string classId, string batchId, AssignBatchTrainersDto dto);
    Task DeleteClass(Guid collegeId, string classId);
    Task DeleteBatch(Guid collegeId, string classId, string batchId);
    Task ApproveUser(Guid collegeId, string userId, UserActionDto dto);
    Task RejectUser(Guid collegeId, string userId, UserActionDto dto);
}
