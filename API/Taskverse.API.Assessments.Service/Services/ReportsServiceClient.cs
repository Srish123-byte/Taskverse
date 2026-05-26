using System.Net;

namespace Taskverse.API.Assessments.Service.Services;

public class ReportsServiceClient : IReportsServiceClient
{
    private readonly HttpClient _httpClient;

    public ReportsServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task EvaluateAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsync(
            $"api/results/evaluate/{attemptId}",
            content: null,
            cancellationToken);

        if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict)
        {
            return;
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException(
            $"Reports service evaluation failed for attempt '{attemptId}' with status code {(int)response.StatusCode}. Response: {detail}");
    }
}
