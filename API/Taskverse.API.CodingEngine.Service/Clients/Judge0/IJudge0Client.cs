using Taskverse.API.CodingEngine.Service.Models;

namespace Taskverse.API.CodingEngine.Service.Clients.Judge0;

public interface IJudge0Client
{
    Task<string> CreateSubmissionAsync(string baseUrl, Judge0CreateSubmissionRequest request, CancellationToken cancellationToken = default);

    Task<Judge0SubmissionResponse> GetSubmissionAsync(string baseUrl, string token, CancellationToken cancellationToken = default);

    Task<Judge0SubmissionResponse> CreateAndWaitAsync(string baseUrl, Judge0CreateSubmissionRequest request, int pollIntervalMs = 200, CancellationToken cancellationToken = default);

    Task<bool> PingAsync(string baseUrl, CancellationToken cancellationToken = default);
}
