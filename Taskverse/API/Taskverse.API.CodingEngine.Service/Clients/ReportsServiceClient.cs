using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Taskverse.API.CodingEngine.Service.Clients;

public class ReportsServiceClient : IReportsServiceClient
{
    private readonly HttpClient _httpClient;

    public ReportsServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<CodingEvaluationResultClientModel?> EvaluateCodingAsync(
        Guid attemptId,
        Guid codingQuestionId,
        decimal score,
        int passedTestCases,
        int totalTestCases,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/reports/coding-evaluate",
            new EvaluateCodingRequestModel(attemptId, codingQuestionId, score, passedTestCases, totalTestCases),
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CodingEvaluationResultClientModel>(cancellationToken);
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(
                $"Reports service rejected coding evaluation for attempt '{attemptId}'. Response: {detail}");
        }

        throw new HttpRequestException(
            $"Reports service coding evaluation failed for attempt '{attemptId}' with status code {(int)response.StatusCode}. Response: {detail}");
    }

    private sealed record EvaluateCodingRequestModel(
        [property: JsonPropertyName("attempt_id")]
        Guid AttemptId,
        [property: JsonPropertyName("coding_question_id")]
        Guid CodingQuestionId,
        [property: JsonPropertyName("score")]
        decimal Score,
        [property: JsonPropertyName("passed_test_cases")]
        int PassedTestCases,
        [property: JsonPropertyName("total_test_cases")]
        int TotalTestCases);
}
