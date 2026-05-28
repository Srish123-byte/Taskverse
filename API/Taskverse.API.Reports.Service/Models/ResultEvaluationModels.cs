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
    [property: JsonPropertyName("generated_at")]
    DateTime GeneratedAt,
    [property: JsonPropertyName("has_pending_coding_evaluation")]
    bool HasPendingCodingEvaluation);

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
        bool hasPendingCodingEvaluation)
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
            result.GeneratedAt,
            hasPendingCodingEvaluation);
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
