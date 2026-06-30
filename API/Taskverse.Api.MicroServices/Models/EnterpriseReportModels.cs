namespace Taskverse.Api.MicroServices.Models;

public record ReportMetadataModel(
    string ReportTitle,
    string GeneratedDate,
    string GeneratedTime,
    string GeneratedBy,
    Dictionary<string, string> AppliedFilters,
    string AcademicYear);

public record CollegeWiseSummaryModel(
    int TotalColleges,
    int TotalStudents,
    int TotalTrainers,
    int TotalAssessments,
    decimal AverageScore,
    decimal OverallPassPercentage);

public record CollegeWiseRowModel(
    string CollegeName,
    int TotalStudents,
    int TotalTrainers,
    int TotalAssessments,
    int AssessmentsCompleted,
    decimal AverageScore,
    decimal HighestScore,
    decimal LowestScore,
    decimal PassPercentage,
    int ActiveStudents,
    string PerformanceGrade);

public record CollegeWiseReportModel(
    ReportMetadataModel Metadata,
    CollegeWiseSummaryModel Summary,
    List<CollegeWiseRowModel> Rows);

public record BranchWiseSummaryModel(
    int TotalBranches,
    int TotalStudents,
    int TotalTrainers,
    int TotalAssessments,
    decimal AverageMarks,
    decimal OverallPassPercentage);

public record BranchWiseRowModel(
    string BranchName,
    int TotalStudents,
    int TotalTrainers,
    int TotalAssessments,
    decimal AverageMarks,
    decimal HighestMarks,
    decimal LowestMarks,
    decimal PassPercentage,
    List<string> StrongestTopics,
    List<string> WeakestTopics);

public record BranchWiseReportModel(
    ReportMetadataModel Metadata,
    BranchWiseSummaryModel Summary,
    List<BranchWiseRowModel> Rows);

public record AssessmentBreakdownModel(
    string AssessmentName,
    string AssessmentType,
    decimal ObtainedMarks,
    decimal TotalMarks,
    decimal Percentage,
    int Rank,
    string Status,
    string Date);

public record StudentAiInsightsModel(
    List<string> LearningGaps,
    List<string> RootCauseAnalysis,
    List<string> WeakTopics,
    List<string> StrongTopics,
    List<string> CommunicationGaps,
    string InterviewReadiness,
    List<string> RecommendedPracticeAreas,
    List<string> SuggestedResources,
    string PriorityLevel,
    List<string> ImprovementPlan);

public record StudentPerformanceRowModel(
    string StudentId,
    string Name,
    string EnrollmentNumber,
    string CollegeName,
    string BranchName,
    string Semester,
    string BatchName,
    string TrainerName,
    List<AssessmentBreakdownModel> Assessments,
    decimal TotalMarks,
    decimal TotalObtained,
    decimal OverallPercentage,
    int OverallRank,
    int CollegeRank,
    int BatchRank,
    decimal CompletionRate,
    string PlacementReadiness,
    string PerformanceTrend,
    StudentAiInsightsModel AiInsights);

public record StudentPerformanceSummaryModel(
    int TotalStudents,
    decimal AveragePercentage,
    decimal PassPercentage,
    decimal HighestPercentage,
    decimal LowestPercentage,
    int PlacementReadyCount);

public record StudentPerformanceReportModel(
    ReportMetadataModel Metadata,
    StudentPerformanceSummaryModel Summary,
    List<StudentPerformanceRowModel> Rows);

public record FilterOptionModel(
    string Id,
    string Name);
