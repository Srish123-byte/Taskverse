namespace Taskverse.Business.DTOs;

public class CollegeDto
{
    public string CollegeId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = default!;
    public string ApprovalStatus { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime RequestedAt { get; set; }
    public string? RequestedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public string? Notes { get; set; }
}

public class CollegeActionDto
{
    public string PerformedBy { get; set; } = default!;
    public string? Reason { get; set; }
}

public class UserActionDto
{
    public string PerformedBy { get; set; } = default!;
    public Guid? PerformedByUserId { get; set; }
    public string? Reason { get; set; }
}

public class PendingUserDto
{
    public string UserId { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Role { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string? InstitutionName { get; set; }
}

public class SuperAdminDashboardDto
{
    public SuperAdminTotalsDto Totals { get; set; } = new();
    public List<CollegeDto> PendingApprovals { get; set; } = [];
    public PlatformHealthDto PlatformHealth { get; set; } = new();
    public List<RecentActivityDto> RecentActivity { get; set; } = [];
    public List<CollegeScoreSummaryDto> AverageScoresByCollege { get; set; } = [];
    public List<UsageTrendPointDto> UsageTrends { get; set; } = [];
}

public class SuperAdminTotalsDto
{
    public int ActiveColleges { get; set; }
    public int RegisteredStudents { get; set; }
    public int AssessmentsThisMonth { get; set; }
    public int AssessmentsPreviousMonth { get; set; }
}

public class PlatformHealthDto
{
    public double UptimePercent { get; set; }
    public double ErrorRatePercent { get; set; }
    public string ApiStatus { get; set; } = default!;
}

public class RecentActivityDto
{
    public string Action { get; set; } = default!;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string PerformedBy { get; set; } = default!;
    public DateTime OccurredAt { get; set; }
    public string? Details { get; set; }
}

public class CollegeScoreSummaryDto
{
    public string CollegeId { get; set; } = default!;
    public string CollegeName { get; set; } = default!;
    public double AverageScore { get; set; }
    public int StudentsAssessed { get; set; }
}

public class UsageTrendPointDto
{
    public DateTime Date { get; set; }
    public int Assessments { get; set; }
    public int StudentsAssessed { get; set; }
}
