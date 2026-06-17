using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Taskverse.API.Assessments.Service.Clients;

public class ReportsServiceClient : IReportsServiceClient
{
    private readonly HttpClient _httpClient;

    public ReportsServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AttemptEvaluationResultClientModel?> EvaluateAttemptAsync(
        Guid attemptId,
        int passingPercentage,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/results/evaluate",
            new EvaluateAttemptRequestModel(attemptId, passingPercentage),
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AttemptEvaluationResultClientModel>(cancellationToken);
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(
                $"Reports service rejected evaluation for attempt '{attemptId}'. Response: {detail}");
        }

        throw new HttpRequestException(
            $"Reports service evaluation failed for attempt '{attemptId}' with status code {(int)response.StatusCode}. Response: {detail}");
    }

    public async Task<StudentAttemptResultClientModel?> GetStudentAttemptResultAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/results/students/attempts/{attemptId}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<StudentAttemptResultClientModel>(cancellationToken);
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Reports service student attempt result fetch failed for attempt '{attemptId}' with status code {(int)response.StatusCode}. Response: {detail}");
    }

    private sealed record EvaluateAttemptRequestModel(
        [property: JsonPropertyName("attempt_id")]
        Guid AttemptId,
        [property: JsonPropertyName("passing_percentage")]
        int PassingPercentage);
}

public sealed record AttemptEvaluationResultClientModel(
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

public sealed record StudentAttemptResultClientModel(
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
    List<StudentAttemptQuestionResultClientModel>? QuestionResults,
    [property: JsonPropertyName("question_explanations")]
    List<StudentAttemptQuestionExplanationClientModel>? QuestionExplanations);

public sealed record StudentAttemptQuestionResultClientModel(
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
    List<string>? UserAnswers,
    [property: JsonPropertyName("correct_answers")]
    List<string>? CorrectAnswers,
    [property: JsonPropertyName("explanation")]
    string? Explanation);

public sealed record StudentAttemptQuestionExplanationClientModel(
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
