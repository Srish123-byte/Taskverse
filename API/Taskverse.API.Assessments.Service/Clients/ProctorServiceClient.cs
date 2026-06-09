using System.Net;
using System.Net.Http.Json;
using Taskverse.API.Assessments.Service.Models;

namespace Taskverse.API.Assessments.Service.Clients;

public class ProctorServiceClient : IProctorServiceClient
{
    private readonly HttpClient _httpClient;

    public ProctorServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProctorSessionStateRecord?> GetSessionByAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"api/v1/proctor/attempts/{attemptId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ProctorSessionStateRecord>(cancellationToken);
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(
                $"Proctor service found an invalid session state for attempt '{attemptId}'. Response: {detail}");
        }

        throw new HttpRequestException(
            $"Proctor service session fetch failed for attempt '{attemptId}' with status code {(int)response.StatusCode}. Response: {detail}");
    }
}
