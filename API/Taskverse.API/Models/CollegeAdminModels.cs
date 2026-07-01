namespace Taskverse.Api.Models;

public class CollegeAdminDashboardResponseModel
{
    public CollegeAdminTotalsResponseModel Totals { get; set; } = new();
    public List<PendingUserResponseModel> PendingApprovals { get; set; } = [];
    public List<RecentActivityResponseModel> RecentActivity { get; set; } = [];
    public List<UsageTrendPointResponseModel> UsageTrends { get; set; } = [];
}

public class CollegeAdminTotalsResponseModel
{
    public int RegisteredStudents { get; set; }
    public int RegisteredTrainers { get; set; }
    public int PendingApprovals { get; set; }
    public int TotalAssessments { get; set; }
    public int AssessmentsThisMonth { get; set; }
    public int AssessmentsPreviousMonth { get; set; }
}

public class ClassConfigurationResponseModel
{
    public ClassConfigurationTotalsResponseModel Totals { get; set; } = new();
    public List<CollegeClassSummaryResponseModel> Classes { get; set; } = [];
}

public class ClassConfigurationTotalsResponseModel
{
    public int TotalClasses { get; set; }
    public int TotalBatches { get; set; }
    public int TotalStudents { get; set; }
    public int CapacityUtilization { get; set; }
}

public class CollegeClassSummaryResponseModel
{
    public string ClassId { get; set; } = string.Empty;
    public string CollegeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? Department { get; set; }
    public int TotalStudents { get; set; }
    public int TotalCapacity { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CollegeBatchSummaryResponseModel> Batches { get; set; } = [];
}

public class CollegeBatchSummaryResponseModel
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
    public List<ApprovedTrainerResponseModel> AssignedTrainers { get; set; } = [];
    public List<ApprovedStudentResponseModel> AssignedStudents { get; set; } = [];
}

public class SubjectOptionResponseModel
{
    public string SubjectId { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
}

public class ApprovedTrainerResponseModel
{
    public string TrainerId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ApprovedStudentResponseModel
{
    public string StudentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CurrentClassId { get; set; }
    public string? CurrentClassName { get; set; }
    public string? CurrentBatchId { get; set; }
    public string? CurrentBatchName { get; set; }
}

public class CreateCollegeClassRequestModel
{
    public string Name { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? Department { get; set; }
}

public class UpdateCollegeClassRequestModel
{
    public string Name { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? Department { get; set; }
}

public class CreateCollegeBatchRequestModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public string? SubjectId { get; set; }
    public string? SubjectName { get; set; }
}

public class UpdateCollegeBatchRequestModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Capacity { get; set; }
    public string? SubjectId { get; set; }
    public string? SubjectName { get; set; }
}

public class AssignBatchTrainersRequestModel
{
    public List<string> TrainerIds { get; set; } = [];
}

public class AssignStudentToBatchRequestModel
{
    public List<string> StudentIds { get; set; } = [];
}
