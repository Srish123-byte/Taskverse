namespace Taskverse.API.Assessments.Service.Services;

public interface IReportsServiceClient
{
    Task EvaluateAttemptAsync(Guid attemptId, int passingPercentage, CancellationToken cancellationToken = default);
}
