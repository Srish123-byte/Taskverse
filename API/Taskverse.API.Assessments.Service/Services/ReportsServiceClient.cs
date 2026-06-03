using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Taskverse.API.Assessments.Service.Services;

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

    private sealed record EvaluateAttemptRequestModel(
        [property: JsonPropertyName("attempt_id")]
        Guid AttemptId,
        [property: JsonPropertyName("passing_percentage")]
        int PassingPercentage);
}
