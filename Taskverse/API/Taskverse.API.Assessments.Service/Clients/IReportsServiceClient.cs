namespace Taskverse.API.Assessments.Service.Clients;

public interface IReportsServiceClient
{
    Task<AttemptEvaluationResultClientModel?> EvaluateAttemptAsync(
        Guid attemptId,
        int passingPercentage,
        CancellationToken cancellationToken = default);

    Task<StudentAttemptResultClientModel?> GetStudentAttemptResultAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default);
}
