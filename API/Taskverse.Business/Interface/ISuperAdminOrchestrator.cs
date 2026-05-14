using Taskverse.Business.DTOs;

namespace Taskverse.Business.Interface;

public interface ISuperAdminOrchestrator
{
    Task<SuperAdminDashboardDto> GetDashboard();
    Task<List<CollegeDto>> GetColleges();
    Task<List<CollegeSearchResultDto>> SearchColleges(CollegeSearchDto dto);
    Task<List<CollegeDto>> GetPendingColleges();
    Task<List<PendingUserDto>> GetPendingUsers();
    Task<CollegeDto> ApproveCollege(string collegeId, CollegeActionDto dto);
    Task<CollegeDto> RejectCollege(string collegeId, CollegeActionDto dto);
    Task<CollegeDto> DeactivateCollege(string collegeId, CollegeActionDto dto);
    Task<CollegeDto> ReactivateCollege(string collegeId, CollegeActionDto dto);
    Task ApproveUser(string userId, UserActionDto dto);
    Task RejectUser(string userId, UserActionDto dto);
}
