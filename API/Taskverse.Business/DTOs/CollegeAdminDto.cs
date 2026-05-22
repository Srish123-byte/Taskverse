namespace Taskverse.Business.DTOs;

public class CollegeAdminDashboardDto
{
    public CollegeAdminTotalsDto Totals { get; set; } = new();
    public List<PendingUserDto> PendingApprovals { get; set; } = [];
    public List<RecentActivityDto> RecentActivity { get; set; } = [];
    public List<UsageTrendPointDto> UsageTrends { get; set; } = [];
}

public class CollegeAdminTotalsDto
{
    public int RegisteredStudents { get; set; }
    public int RegisteredTrainers { get; set; }
    public int PendingApprovals { get; set; }
    public int AssessmentsThisMonth { get; set; }
    public int AssessmentsPreviousMonth { get; set; }
}

public class ClassConfigurationDto
{
    public ClassConfigurationTotalsDto Totals { get; set; } = new();
    public List<CollegeClassSummaryDto> Classes { get; set; } = [];
}

public class ClassConfigurationTotalsDto
{
    public int TotalClasses { get; set; }
    public int TotalBatches { get; set; }
    public int TotalStudents { get; set; }
    public int CapacityUtilization { get; set; }
}

public class CollegeClassSummaryDto
{
    public string ClassId { get; set; } = string.Empty;
    public string CollegeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? Department { get; set; }
    public int TotalStudents { get; set; }
    public int TotalCapacity { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CollegeBatchSummaryDto> Batches { get; set; } = [];
}

public class CollegeBatchSummaryDto
{
    public string BatchId { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string CollegeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public int Capacity { get; set; }
    public int StudentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ApprovedTrainerDto> AssignedTrainers { get; set; } = [];
}

public class SubjectOptionDto
{
    public string SubjectId { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
}

public class ApprovedTrainerDto
{
    public string TrainerId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AssignBatchTrainersDto
{
    public List<string> TrainerIds { get; set; } = [];
}

public class CreateCollegeClassDto
{
    public string Name { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? Department { get; set; }
}

public class CreateCollegeBatchDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public string? SubjectId { get; set; }
    public string? SubjectName { get; set; }
}
