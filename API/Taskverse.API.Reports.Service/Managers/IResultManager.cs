using Taskverse.Data.DataAccess;
using Taskverse.API.Reports.Service.Models;

namespace Taskverse.API.Reports.Service.Managers;

public interface IResultManager
{
    Task<bool> ResultExistsForAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default);

    Task<Attempt?> GetAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default);

    Task<Assessment?> GetAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken = default);

    Task<List<AttemptAnswer>> GetAttemptAnswersAsync(Guid attemptId, CancellationToken cancellationToken = default);

    Task<List<SubmittedAttemptScoreSnapshot>> GetSubmittedAttemptScoreSnapshotsAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task PersistAttemptEvaluationAsync(
        Attempt attempt,
        Result result,
        IReadOnlyDictionary<Guid, int> rankByAttemptId,
        CancellationToken cancellationToken = default);

    Task<List<StudentResultResponse>> GetStudentResultsAsync(
        Guid studentId,
        CancellationToken cancellationToken = default);
}
