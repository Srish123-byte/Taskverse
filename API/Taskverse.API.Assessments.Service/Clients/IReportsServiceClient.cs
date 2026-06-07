namespace Taskverse.API.Assessments.Service.Clients;

public interface IReportsServiceClient
{
    Task EvaluateAttemptAsync(Guid attemptId, int passingPercentage, CancellationToken cancellationToken = default);

    Task<StudentAttemptResultClientModel?> GetStudentAttemptResultAsync(
        Guid studentId,
        Guid attemptId,
        CancellationToken cancellationToken = default);
}
