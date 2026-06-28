namespace Taskverse.Api.Models;

public class CollegeResponseModel
{
    public string CollegeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AdminName { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? RequestedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public string? Notes { get; set; }
}

public class CollegeActionRequestModel
{
    public string? Reason { get; set; }
}

public class CollegeSearchRequestModel
{
    public string? Query { get; set; }
    public string Status { get; set; } = "all";
}

public class CollegeSearchResponseModel
{
    public string CollegeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public string? AdminName { get; set; }
    public string? AdminEmail { get; set; }
    public int TotalUsers { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class UserActionRequestModel
{
    public string? Reason { get; set; }
}

public class UserSearchRequestModel
{
    public string? Status { get; set; }
    public string? Role { get; set; }
    public string? SearchTerm { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PendingUserResponseModel
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? InstitutionName { get; set; }
}

public class PagedUserSearchResponseModel
{
    public List<PendingUserResponseModel> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class BulkStudentUploadRequestModel
{
    public List<BulkStudentUploadRowRequestModel> Rows { get; set; } = [];
}

public class BulkStudentUploadRowRequestModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string EnrollmentNumber { get; set; } = string.Empty;
    public string CollegeId { get; set; } = string.Empty;
    public string CollegeName { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string BatchId { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
}

public class BulkStudentUploadResultResponseModel
{
    public int CreatedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int InvalidCount { get; set; }
    public List<BulkStudentUploadCreatedUserResponseModel> CreatedUsers { get; set; } = [];
    public List<BulkStudentUploadRowIssueResponseModel> DuplicateRows { get; set; } = [];
    public List<BulkStudentUploadRowIssueResponseModel> InvalidRows { get; set; } = [];
}

public class BulkStudentUploadCreatedUserResponseModel
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class BulkStudentUploadRowIssueResponseModel
{
    public int RowNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class SuperAdminDashboardResponseModel
{
    public SuperAdminTotalsResponseModel Totals { get; set; } = new();
    public List<CollegeResponseModel> PendingApprovals { get; set; } = [];
    public PlatformHealthResponseModel PlatformHealth { get; set; } = new();
    public List<RecentActivityResponseModel> RecentActivity { get; set; } = [];
    public List<CollegeScoreSummaryResponseModel> AverageScoresByCollege { get; set; } = [];
    public List<UsageTrendPointResponseModel> UsageTrends { get; set; } = [];
}

public class SuperAdminTotalsResponseModel
{
    public int ActiveColleges { get; set; }
    public int RegisteredStudents { get; set; }
    public int AssessmentsThisMonth { get; set; }
    public int AssessmentsPreviousMonth { get; set; }
}

public class PlatformHealthResponseModel
{
    public double UptimePercent { get; set; }
    public double ErrorRatePercent { get; set; }
    public string ApiStatus { get; set; } = string.Empty;
}

public class RecentActivityResponseModel
{
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? Details { get; set; }
}

public class CollegeScoreSummaryResponseModel
{
    public string CollegeId { get; set; } = string.Empty;
    public string CollegeName { get; set; } = string.Empty;
    public double AverageScore { get; set; }
    public int StudentsAssessed { get; set; }
}

public class UsageTrendPointResponseModel
{
    public DateTime Date { get; set; }
    public int Assessments { get; set; }
    public int StudentsAssessed { get; set; }
}
