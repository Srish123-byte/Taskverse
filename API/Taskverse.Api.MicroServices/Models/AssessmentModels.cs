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

public record CreateQuestionModel(
    Guid CollegeId,
    string CreatedBy,
    string Stream,
    string Subject,
    string Topic,
    string TopicTag,
    string QuestionType,
    string QuestionText,
    List<string>? Options,
    string Answer,
    string? Explanation,
    decimal Marks,
    decimal NegativeMarks,
    int DifficultyLevel);

public record DeleteQuestionsModel(
    string CreatedBy,
    List<Guid> QuestionIds);

public record AssessmentQuestionModel(
    Guid QuestionId,
    Guid CollegeId,
    string? Stream,
    string? Subject,
    string? Topic,
    string? TopicTag,
    string QuestionType,
    string QuestionText,
    List<string>? Options,
    string? Answer,
    string? Explanation,
    decimal Marks,
    decimal NegativeMarks,
    int DifficultyLevel,
    int Version,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? ModifiedAt);
