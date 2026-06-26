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
    DateTime? SubmittedAt,
    DateTime GeneratedAt,
    int DurationMinutes,
    int TotalQuestions,
    int AttemptedQuestions,
    int CorrectAnswers,
    int WrongAnswers,
    int UnansweredQuestions,
    int ParticipantCount,
    bool HasPendingCodingEvaluation,
    List<StudentResultQuestionResultModel>? QuestionResults,
    List<StudentResultQuestionExplanationModel>? QuestionExplanations);

public record StudentResultQuestionResultModel(
    Guid QuestionId,
    int DisplayOrder,
    string QuestionType,
    string QuestionText,
    decimal Marks,
    decimal AwardedMarks,
    string Status,
    List<string>? UserAnswers,
    List<string>? CorrectAnswers,
    string? Explanation);

public record StudentResultQuestionExplanationModel(
    Guid QuestionId,
    int DisplayOrder,
    string QuestionType,
    string QuestionText,
    string? Explanation);
