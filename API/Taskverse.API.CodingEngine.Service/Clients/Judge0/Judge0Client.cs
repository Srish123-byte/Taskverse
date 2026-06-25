using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Taskverse.API.CodingEngine.Service.Models;

namespace Taskverse.API.CodingEngine.Service.Clients.Judge0;

public class Judge0Client : IJudge0Client
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Judge0Client> _logger;

    public Judge0Client(HttpClient httpClient, ILogger<Judge0Client> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> CreateSubmissionAsync(string baseUrl, Judge0CreateSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            BuildUri(baseUrl, "submissions?base64_encoded=false&wait=false"), request, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<Judge0SubmissionResponse>(cancellationToken: cancellationToken);
        var token = result?.Token;

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Judge0 returned an empty token.");
        }

        _logger.LogDebug("Created Judge0 submission with token '{Token}' for language {LanguageId}.", token, request.LanguageId);
        return token;
    }

    public async Task<Judge0SubmissionResponse> GetSubmissionAsync(string baseUrl, string token, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            BuildUri(baseUrl, $"submissions/{token}?base64_encoded=false&fields=*"), cancellationToken);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Judge0SubmissionResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException($"Judge0 returned empty response for token '{token}'.");
    }

    public async Task<Judge0SubmissionResponse> CreateAndWaitAsync(
        string baseUrl,
        Judge0CreateSubmissionRequest request,
        int pollIntervalMs = 200,
        CancellationToken cancellationToken = default)
    {
        var token = await CreateSubmissionAsync(baseUrl, request, cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await GetSubmissionAsync(baseUrl, token, cancellationToken);

            if (result.Status is null)
            {
                await Task.Delay(pollIntervalMs, cancellationToken);
                continue;
            }

            if (result.Status.Id != Judge0StatusCodes.InQueue && result.Status.Id != Judge0StatusCodes.Processing)
            {
                _logger.LogDebug(
                    "Judge0 submission '{Token}' finished with status {StatusId} ({Description}).",
                    token, result.Status.Id, result.Status.Description);

                return result;
            }

            await Task.Delay(pollIntervalMs, cancellationToken);
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new TimeoutException($"Judge0 submission '{token}' did not complete before cancellation.");
    }

    public async Task<bool> PingAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(BuildUri(baseUrl, "languages"), cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Judge0 health ping failed for '{BaseUrl}'.", baseUrl);
            return false;
        }
    }

    private static Uri BuildUri(string baseUrl, string relativePath)
    {
        var normalizedBase = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        return new Uri(new Uri(normalizedBase, UriKind.Absolute), relativePath);
    }
}
