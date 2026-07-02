using Taskverse.API.College.Service.Models;

namespace Taskverse.API.College.Service.Services;

public interface ICollegeService
{
    Task<List<RegistrationCollegeRecord>> GetApprovedRegistrationColleges();
    Task<List<RegistrationClassRecord>> GetRegistrationClasses(Guid collegeId);
    Task<List<RegistrationBatchRecord>> GetRegistrationBatches(Guid classId);
    Task<List<CollegeSearchResultRecord>> SearchColleges(CollegeSearchRequest request);
    Task<List<CollegeRecord>> GetColleges();
    Task<List<CollegeRecord>> GetPendingColleges();
    Task<CollegeRecord?> GetCollege(Guid collegeId);
    Task<CollegeRecord?> ApproveCollege(Guid collegeId, CollegeActionRequest request);
    Task<CollegeRecord?> RejectCollege(Guid collegeId, CollegeActionRequest request);
    Task<CollegeRecord?> DeactivateCollege(Guid collegeId, CollegeActionRequest request);
    Task<CollegeRecord?> ReactivateCollege(Guid collegeId, CollegeActionRequest request);
}
