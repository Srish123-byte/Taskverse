using Taskverse.API.College.Service.DTOs;

namespace Taskverse.API.College.Service.Orchestrators;

public interface ICollegeOrchestrator
{
    Task<List<PendingUserDto>> GetPendingUsersByCollege(Guid collegeId);
    Task<List<PendingUserDto>> GetPendingUsersForCollegeAdmin(Guid collegeAdminUserId);
    Task<List<ApprovedTrainerDto>> GetApprovedTrainersByCollege(Guid collegeId);
    Task<List<SubjectOptionDto>> GetSubjects();
    Task<CollegeClassSummaryDto> CreateClass(Guid collegeId, CreateCollegeClassDto dto);
    Task<CollegeClassSummaryDto> UpdateClass(Guid collegeId, Guid classId, UpdateCollegeClassDto dto);
    Task<CollegeBatchSummaryDto> CreateBatch(Guid collegeId, Guid classId, CreateCollegeBatchDto dto);
    Task<CollegeBatchSummaryDto> UpdateBatch(Guid collegeId, Guid classId, Guid batchId, UpdateCollegeBatchDto dto);
    Task<CollegeBatchSummaryDto> AssignBatchTrainers(Guid collegeId, Guid classId, Guid batchId, AssignBatchTrainersDto dto);
    Task DeleteClass(Guid collegeId, Guid classId);
    Task DeleteBatch(Guid collegeId, Guid classId, Guid batchId);
    Task ApproveUser(Guid collegeId, string userId, CollegeUserActionDto dto);
    Task RejectUser(Guid collegeId, string userId, CollegeUserActionDto dto);
}
