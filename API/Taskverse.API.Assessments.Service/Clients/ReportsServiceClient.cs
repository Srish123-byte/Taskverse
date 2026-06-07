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

    public async Task EvaluateAttemptAsync(
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
            return;
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
        Guid studentId,
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(
            $"api/results/students/{studentId}/attempts/{attemptId}",
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
            $"Reports service student attempt result fetch failed for student '{studentId}' and attempt '{attemptId}' with status code {(int)response.StatusCode}. Response: {detail}");
    }

    private sealed record EvaluateAttemptRequestModel(
        [property: JsonPropertyName("attempt_id")]
        Guid AttemptId,
        [property: JsonPropertyName("passing_percentage")]
        int PassingPercentage);
}

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
    [property: JsonPropertyName("generated_at")]
    DateTime GeneratedAt,
    [property: JsonPropertyName("has_pending_coding_evaluation")]
    bool HasPendingCodingEvaluation);
