using Taskverse.API.Reports.Service.Models;

namespace Taskverse.API.Reports.Service.Services;

public interface IAttemptEvaluationService
{
    Task<AttemptEvaluationExecutionResult> EvaluateAttemptAsync(
        Guid attemptId,
        int passingPercentage,
        CancellationToken cancellationToken = default);
}
