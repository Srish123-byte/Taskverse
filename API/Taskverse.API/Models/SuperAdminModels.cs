namespace Taskverse.Api.Models;

public class CollegeResponseModel
{
    public string CollegeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
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
