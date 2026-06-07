using Taskverse.API.Reports.Service.Models;

namespace Taskverse.API.Reports.Service.Orchestrators;

public interface IResultOrchestrator
{
    Task<AttemptEvaluationExecutionResult> EvaluateAttemptAsync(
        Guid attemptId,
        int passingPercentage,
        CancellationToken cancellationToken = default);

    Task<List<StudentResultResponse>> GetStudentResultsAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);

    Task<StudentResultResponse> GetStudentAttemptResultAsync(
        Guid studentId,
        Guid attemptId,
        CancellationToken cancellationToken = default);
}
