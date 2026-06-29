using Newtonsoft.Json;

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
    [property: JsonProperty("result_id")]
    Guid ResultId,
    [property: JsonProperty("assessment_id")]
    Guid AssessmentId,
    [property: JsonProperty("assessment_name")]
    string AssessmentName,
    [property: JsonProperty("attempt_id")]
    Guid AttemptId,
    [property: JsonProperty("student_id")]
    Guid StudentId,
    [property: JsonProperty("total_marks")]
    decimal TotalMarks,
    [property: JsonProperty("obtained_marks")]
    decimal ObtainedMarks,
    [property: JsonProperty("percentage")]
    decimal Percentage,
    [property: JsonProperty("rank")]
    int Rank,
    [property: JsonProperty("result_status")]
    string ResultStatus,
    [property: JsonProperty("submitted_at")]
    DateTime? SubmittedAt,
    [property: JsonProperty("generated_at")]
    DateTime GeneratedAt,
    [property: JsonProperty("duration_minutes")]
    int DurationMinutes,
    [property: JsonProperty("total_questions")]
    int TotalQuestions,
    [property: JsonProperty("attempted_questions")]
    int AttemptedQuestions,
    [property: JsonProperty("correct_answers")]
    int CorrectAnswers,
    [property: JsonProperty("wrong_answers")]
    int WrongAnswers,
    [property: JsonProperty("unanswered_questions")]
    int UnansweredQuestions,
    [property: JsonProperty("participant_count")]
    int ParticipantCount,
    [property: JsonProperty("has_pending_coding_evaluation")]
    bool HasPendingCodingEvaluation,
    [property: JsonProperty("show_results_immediately")]
    bool ShowResultsImmediately,
    [property: JsonProperty("question_results")]
    List<StudentResultQuestionResultModel>? QuestionResults,
    [property: JsonProperty("question_explanations")]
    List<StudentResultQuestionExplanationModel>? QuestionExplanations);

public record StudentResultQuestionResultModel(
    [property: JsonProperty("question_id")]
    Guid QuestionId,
    [property: JsonProperty("display_order")]
    int DisplayOrder,
    [property: JsonProperty("question_type")]
    string QuestionType,
    [property: JsonProperty("question_text")]
    string QuestionText,
    [property: JsonProperty("marks")]
    decimal Marks,
    [property: JsonProperty("awarded_marks")]
    decimal AwardedMarks,
    [property: JsonProperty("status")]
    string Status,
    [property: JsonProperty("user_answers")]
    List<string>? UserAnswers,
    [property: JsonProperty("correct_answers")]
    List<string>? CorrectAnswers,
    [property: JsonProperty("explanation")]
    string? Explanation);

public record StudentResultQuestionExplanationModel(
    [property: JsonProperty("question_id")]
    Guid QuestionId,
    [property: JsonProperty("display_order")]
    int DisplayOrder,
    [property: JsonProperty("question_type")]
    string QuestionType,
    [property: JsonProperty("question_text")]
    string QuestionText,
    [property: JsonProperty("explanation")]
    string? Explanation);
