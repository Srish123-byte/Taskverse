using Taskverse.API.Reports.Service.Managers;
using Taskverse.API.Reports.Service.Models;
using Taskverse.API.Reports.Service.Services;

namespace Taskverse.API.Reports.Service.Orchestrators;

public class ResultOrchestrator : IResultOrchestrator
{
    private readonly IAttemptEvaluationService _attemptEvaluationService;
    private readonly IResultManager _resultManager;

    public ResultOrchestrator(
        IAttemptEvaluationService attemptEvaluationService,
        IResultManager resultManager)
    {
        _attemptEvaluationService = attemptEvaluationService;
        _resultManager = resultManager;
    }

    public Task<AttemptEvaluationExecutionResult> EvaluateAttemptAsync(
        Guid attemptId,
        int passingPercentage,
        CancellationToken cancellationToken = default)
    {
        return _attemptEvaluationService.EvaluateAttemptAsync(attemptId, passingPercentage, cancellationToken);
    }

    public Task<List<StudentResultResponse>> GetStudentResultsAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return _resultManager.GetStudentResultsAsync(studentId, cancellationToken);
    }
}
