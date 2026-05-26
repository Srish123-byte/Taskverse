namespace Taskverse.Api.MicroServices.Models;

public record ReportModel(
    string ReportId,
    string Type,
    string GeneratedFor,
    DateTime GeneratedAt,
    string Status,
    string? DownloadUrl);

public record GenerateReportRequestModel(
    string Type,
    string UserId,
    string? AssessmentId,
    string? ExamId,
    DateTime? DateFrom,
    DateTime? DateTo);

public record UserPerformanceReportModel(
    string UserId,
    int TotalAssessments,
    int Completed,
    double AverageScore,
    int HighestScore,
    int LowestScore,
    DateTime ReportGeneratedAt);

public record AssessmentReportModel(
    string AssessmentId,
    string Title,
    int TotalParticipants,
    double AverageScore,
    double PassRate,
    DateTime ReportGeneratedAt);

public record StudentResultModel(
    Guid ResultId,
    Guid AssessmentId,
    string AssessmentName,
    Guid AttemptId,
    Guid StudentId,
    decimal TotalMarks,
    decimal ObtainedMarks,
    decimal Percentage,
    int Rank,
    string ResultStatus,
    DateTime GeneratedAt,
    bool HasPendingCodingEvaluation);
