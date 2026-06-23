using Taskverse.API.CodingEngine.Worker.Service.Models;

namespace Taskverse.API.CodingEngine.Worker.Service.Services;

public interface IJudge0Client
{
    Task<string> CreateSubmissionAsync(Judge0CreateSubmissionRequest request, CancellationToken cancellationToken = default);

    Task<Judge0SubmissionResponse> GetSubmissionAsync(string token, CancellationToken cancellationToken = default);

    Task<Judge0SubmissionResponse> CreateAndWaitAsync(Judge0CreateSubmissionRequest request, int pollIntervalMs = 200, CancellationToken cancellationToken = default);
}
