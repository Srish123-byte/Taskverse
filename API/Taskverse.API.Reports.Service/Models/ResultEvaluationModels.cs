using System.Text.Json.Serialization;
using Taskverse.Data.Enums;

namespace Taskverse.API.Reports.Service.Models;

public record AttemptResultResponse(
    [property: JsonPropertyName("result_id")]
    Guid ResultId,
    [property: JsonPropertyName("assessment_id")]
    Guid AssessmentId,
    [property: JsonPropertyName("attempt_id")]
    Guid AttemptId,
    [property: JsonPropertyName("student_id")]
    Guid StudentId,
    [property: JsonPropertyName("total_marks")]
    decimal TotalMarks,
    [property: JsonPropertyName("obtained_marks")]
    decimal ObtainedMarks,
    [property: JsonPropertyName("percentage")]
    decimal Percentage,
    [property: JsonPropertyName("rank")]
    int Rank,
    [property: JsonPropertyName("result_status")]
    string ResultStatus,
    [property: JsonPropertyName("generated_at")]
    DateTime GeneratedAt,
    [property: JsonPropertyName("has_pending_coding_evaluation")]
    bool HasPendingCodingEvaluation);

public record EvaluateAttemptRequest(
    [property: JsonPropertyName("attempt_id")]
    Guid AttemptId,
    [property: JsonPropertyName("passing_percentage")]
    int PassingPercentage);

public record AttemptEvaluationExecutionResult(
    AttemptResultResponse? Result,
    bool WasSkipped)
{
    public static AttemptEvaluationExecutionResult Completed(AttemptResultResponse result)
        => new(result, false);

    public static AttemptEvaluationExecutionResult Skipped()
        => new(null, true);
}

public record StudentResultResponse(
    [property: JsonPropertyName("result_id")]
    Guid ResultId,
    [property: JsonPropertyName("assessment_id")]
    Guid AssessmentId,
    [property: JsonPropertyName("assessment_name")]
    string AssessmentName,
    [property: JsonPropertyName("attempt_id")]
    Guid AttemptId,
    [property: JsonPropertyName("student_id")]
    Guid StudentId,
    [property: JsonPropertyName("total_marks")]
    decimal TotalMarks,
    [property: JsonPropertyName("obtained_marks")]
    decimal ObtainedMarks,
    [property: JsonPropertyName("percentage")]
    decimal Percentage,
    [property: JsonPropertyName("rank")]
    int Rank,
    [property: JsonPropertyName("result_status")]
    string ResultStatus,
    [property: JsonPropertyName("submitted_at")]
    DateTime? SubmittedAt,
    [property: JsonPropertyName("generated_at")]
    DateTime GeneratedAt,
    [property: JsonPropertyName("duration_minutes")]
    int DurationMinutes,
    [property: JsonPropertyName("total_questions")]
    int TotalQuestions,
    [property: JsonPropertyName("attempted_questions")]
    int AttemptedQuestions,
    [property: JsonPropertyName("correct_answers")]
    int CorrectAnswers,
    [property: JsonPropertyName("wrong_answers")]
    int WrongAnswers,
    [property: JsonPropertyName("unanswered_questions")]
    int UnansweredQuestions,
    [property: JsonPropertyName("participant_count")]
    int ParticipantCount,
    [property: JsonPropertyName("has_pending_coding_evaluation")]
    bool HasPendingCodingEvaluation,
    [property: JsonPropertyName("question_results")]
    List<StudentResultQuestionResultResponse> QuestionResults,
    [property: JsonPropertyName("question_explanations")]
    List<StudentResultQuestionExplanationResponse> QuestionExplanations);

public record StudentResultQuestionResultResponse(
    [property: JsonPropertyName("question_id")]
    Guid QuestionId,
    [property: JsonPropertyName("display_order")]
    int DisplayOrder,
    [property: JsonPropertyName("question_type")]
    string QuestionType,
    [property: JsonPropertyName("question_text")]
    string QuestionText,
    [property: JsonPropertyName("marks")]
    decimal Marks,
    [property: JsonPropertyName("awarded_marks")]
    decimal AwardedMarks,
    [property: JsonPropertyName("status")]
    string Status,
    [property: JsonPropertyName("user_answers")]
    List<string> UserAnswers,
    [property: JsonPropertyName("correct_answers")]
    List<string> CorrectAnswers,
    [property: JsonPropertyName("explanation")]
    string? Explanation);

public record StudentResultQuestionExplanationResponse(
    [property: JsonPropertyName("question_id")]
    Guid QuestionId,
    [property: JsonPropertyName("display_order")]
    int DisplayOrder,
    [property: JsonPropertyName("question_type")]
    string QuestionType,
    [property: JsonPropertyName("question_text")]
    string QuestionText,
    [property: JsonPropertyName("explanation")]
    string? Explanation);

public static class ResultMappings
{
    public static AttemptResultResponse ToAttemptResultResponse(
        this Taskverse.Data.DataAccess.Result result,
        bool hasPendingCodingEvaluation)
    {
        return new AttemptResultResponse(
            result.ResultId,
            result.AssessmentId,
            result.AttemptId,
            result.StudentId,
            result.TotalMarks,
            result.ObtainedMarks,
            result.Percentage,
            result.Rank,
            result.ResultStatus.ToString().ToUpperInvariant(),
            result.GeneratedAt,
            hasPendingCodingEvaluation);
    }

    public static StudentResultResponse ToStudentResultResponse(
        this Taskverse.Data.DataAccess.Result result,
        string assessmentName,
        DateTime? submittedAt,
        int durationMinutes,
        int totalQuestions,
        int attemptedQuestions,
        int correctAnswers,
        int wrongAnswers,
        int unansweredQuestions,
        int participantCount,
        bool hasPendingCodingEvaluation,
        List<StudentResultQuestionResultResponse>? questionResults = null,
        List<StudentResultQuestionExplanationResponse>? questionExplanations = null)
    {
        return new StudentResultResponse(
            result.ResultId,
            result.AssessmentId,
            assessmentName,
            result.AttemptId,
            result.StudentId,
            result.TotalMarks,
            result.ObtainedMarks,
            result.Percentage,
            result.Rank,
            result.ResultStatus.ToString().ToUpperInvariant(),
            submittedAt,
            result.GeneratedAt,
            durationMinutes,
            totalQuestions,
            attemptedQuestions,
            correctAnswers,
            wrongAnswers,
            unansweredQuestions,
            participantCount,
            hasPendingCodingEvaluation,
            questionResults ?? [],
            questionExplanations ?? []);
    }
}

public record AssessmentQuestionEvaluationContext(
    Guid QuestionId,
    string QuestionType,
    string? CorrectAnswer,
    decimal Marks,
    decimal NegativeMarks);

public record QuestionEvaluationResult(
    bool IsPending,
    bool IsAnswered,
    bool IsCorrect,
    decimal AwardedMarks,
    bool ShouldUpdateAttemptAnswer);

public record AttemptEvaluationSummary(
    decimal ObtainedMarks,
    decimal Percentage,
    ResultStatus ResultStatus,
    bool HasPendingCodingEvaluation);
