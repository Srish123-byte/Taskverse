namespace Taskverse.Api.MicroServices.Models;

public record AssessmentModel(
    string AssessmentId,
    string Title,
    string? Description,
    string Type,
    string? ExamId,
    List<string>? ChallengeIds,
    List<string> AssignedTo,
    DateTime? DueDate,
    bool IsActive,
    string CreatedBy,
    DateTime CreatedAt);

public record AssessmentResultModel(
    string ResultId,
    string AssessmentId,
    string UserId,
    string Status,
    int? Score,
    DateTime? CompletedAt,
    ExamResultModel? ExamResult,
    List<CodeExecutionResultModel>? CodingResults);

public record CreateAssessmentModel(
    string Title,
    string? Description,
    string Type,
    string? ExamId,
    List<string>? ChallengeIds,
    List<string> AssignedTo,
    DateTime? DueDate,
    string CreatedBy);

public record AssessmentSummaryModel(
    string AssessmentId,
    string Title,
    int TotalAssigned,
    int TotalCompleted,
    double? AverageScore);
