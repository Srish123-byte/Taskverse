using Taskverse.API.Reports.Service.Models;

namespace Taskverse.API.Reports.Service.Managers;

public interface IResultManager
{
    Task<AttemptResultResponse> EvaluateAttemptAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default);

    Task<AttemptResultResponse> GetAttemptResultAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default);

    Task<List<StudentResultResponse>> GetStudentResultsAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);
}
