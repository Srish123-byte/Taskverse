using Taskverse.Data.Enums;

namespace Taskverse.API.Reports.Service.Models;

public class ReportMetadataResponse
{
    public string ReportTitle { get; set; } = "";
    public string GeneratedDate { get; set; } = "";
    public string GeneratedTime { get; set; } = "";
    public string GeneratedBy { get; set; } = "";
    public Dictionary<string, string> AppliedFilters { get; set; } = new();
    public string AcademicYear { get; set; } = "";
}

// ── Super Admin ──
public class CollegeWiseSummaryResponse
{
    public int TotalColleges { get; set; }
    public int TotalStudents { get; set; }
    public int TotalTrainers { get; set; }
    public int TotalAssessments { get; set; }
    public decimal AverageScore { get; set; }
    public decimal OverallPassPercentage { get; set; }
}

public class CollegeWiseRowResponse
{
    public string CollegeName { get; set; } = "";
    public int TotalStudents { get; set; }
    public int TotalTrainers { get; set; }
    public int TotalAssessments { get; set; }
    public int AssessmentsCompleted { get; set; }
    public decimal AverageScore { get; set; }
    public decimal HighestScore { get; set; }
    public decimal LowestScore { get; set; }
    public decimal PassPercentage { get; set; }
    public int ActiveStudents { get; set; }
    public string PerformanceGrade { get; set; } = "";
}

public class CollegeWiseReportResponse
{
    public ReportMetadataResponse Metadata { get; set; } = new();
    public CollegeWiseSummaryResponse Summary { get; set; } = new();
    public List<CollegeWiseRowResponse> Rows { get; set; } = new();
}

// ── College Admin ──
public class BranchWiseSummaryResponse
{
    public int TotalBranches { get; set; }
    public int TotalStudents { get; set; }
    public int TotalTrainers { get; set; }
    public int TotalAssessments { get; set; }
    public decimal AverageMarks { get; set; }
    public decimal OverallPassPercentage { get; set; }
}

public class BranchWiseRowResponse
{
    public string BranchName { get; set; } = "";
    public int TotalStudents { get; set; }
    public int TotalTrainers { get; set; }
    public int TotalAssessments { get; set; }
    public decimal AverageMarks { get; set; }
    public decimal HighestMarks { get; set; }
    public decimal LowestMarks { get; set; }
    public decimal PassPercentage { get; set; }
    public List<string> StrongestTopics { get; set; } = new();
    public List<string> WeakestTopics { get; set; } = new();
}

public class BranchWiseReportResponse
{
    public ReportMetadataResponse Metadata { get; set; } = new();
    public BranchWiseSummaryResponse Summary { get; set; } = new();
    public List<BranchWiseRowResponse> Rows { get; set; } = new();
}

// ── Trainer ──
public class AssessmentBreakdownResponse
{
    public string AssessmentName { get; set; } = "";
    public string AssessmentType { get; set; } = "";
    public decimal ObtainedMarks { get; set; }
    public decimal TotalMarks { get; set; }
    public decimal Percentage { get; set; }
    public int Rank { get; set; }
    public string Status { get; set; } = "";
    public string Date { get; set; } = "";
}

public class StudentAiInsightsResponse
{
    public List<string> LearningGaps { get; set; } = new();
    public List<string> RootCauseAnalysis { get; set; } = new();
    public List<string> WeakTopics { get; set; } = new();
    public List<string> StrongTopics { get; set; } = new();
    public List<string> CommunicationGaps { get; set; } = new();
    public string InterviewReadiness { get; set; } = "";
    public List<string> RecommendedPracticeAreas { get; set; } = new();
    public List<string> SuggestedResources { get; set; } = new();
    public string PriorityLevel { get; set; } = "";
    public List<string> ImprovementPlan { get; set; } = new();
}

public class StudentPerformanceRowResponse
{
    public string StudentId { get; set; } = "";
    public string Name { get; set; } = "";
    public string EnrollmentNumber { get; set; } = "";
    public string CollegeName { get; set; } = "";
    public string BranchName { get; set; } = "";
    public string Semester { get; set; } = "";
    public string BatchName { get; set; } = "";
    public string TrainerName { get; set; } = "";
    public List<AssessmentBreakdownResponse> Assessments { get; set; } = new();
    public decimal TotalMarks { get; set; }
    public decimal TotalObtained { get; set; }
    public decimal OverallPercentage { get; set; }
    public int OverallRank { get; set; }
    public int CollegeRank { get; set; }
    public int BatchRank { get; set; }
    public decimal CompletionRate { get; set; }
    public string PlacementReadiness { get; set; } = "";
    public string PerformanceTrend { get; set; } = "";
    public StudentAiInsightsResponse AiInsights { get; set; } = new();
}

public class StudentPerformanceSummaryResponse
{
    public int TotalStudents { get; set; }
    public decimal AveragePercentage { get; set; }
    public decimal PassPercentage { get; set; }
    public decimal HighestPercentage { get; set; }
    public decimal LowestPercentage { get; set; }
    public int PlacementReadyCount { get; set; }
}

public class StudentPerformanceReportResponse
{
    public ReportMetadataResponse Metadata { get; set; } = new();
    public StudentPerformanceSummaryResponse Summary { get; set; } = new();
    public List<StudentPerformanceRowResponse> Rows { get; set; } = new();
}

public class FilterOptionResponse
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
}
