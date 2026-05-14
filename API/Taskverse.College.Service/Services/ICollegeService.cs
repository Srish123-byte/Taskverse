using Taskverse.API.College.Service.Models;

namespace Taskverse.API.College.Service.Services;

public interface ICollegeService
{
    Task<List<RegistrationCollegeRecord>> GetApprovedRegistrationColleges();
    Task<List<RegistrationClassRecord>> GetRegistrationClasses(Guid collegeId);
    Task<List<RegistrationBatchRecord>> GetRegistrationBatches(Guid classId);
    Task<List<CollegeSearchResultRecord>> SearchColleges(CollegeSearchRequest request);
    IReadOnlyList<CollegeRecord> GetColleges();
    List<CollegeRecord> GetPendingColleges();
    CollegeRecord? GetCollege(Guid collegeId);
    CollegeRecord? ApproveCollege(Guid collegeId, CollegeActionRequest request);
    CollegeRecord? RejectCollege(Guid collegeId, CollegeActionRequest request);
    CollegeRecord? DeactivateCollege(Guid collegeId, CollegeActionRequest request);
    CollegeRecord? ReactivateCollege(Guid collegeId, CollegeActionRequest request);
}
