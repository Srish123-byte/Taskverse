using Taskverse.API.Reports.Service.Models;

namespace Taskverse.API.Reports.Service.Services;

public interface IResultEvaluationService
{
    Task<AttemptResultResponse> EvaluateAttemptAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default);
}
