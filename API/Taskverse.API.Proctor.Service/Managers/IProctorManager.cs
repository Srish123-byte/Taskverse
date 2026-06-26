using Taskverse.Data.DataAccess;

namespace Taskverse.API.Proctor.Service.Managers;

public interface IProctorManager
{
    Task<Student?> GetStudentByUserIdAsync(Guid studentUserId);
    Task<Attempt?> GetAttemptForStudentAsync(Guid attemptId, Guid studentId);
    Task<ProctoringSession?> GetActiveSessionForAttemptAsync(Guid attemptId, Guid studentId);
    Task<List<ProctoringSession>> GetSessionsByAttemptAsync(Guid attemptId);
    Task<ProctoringSession?> GetSessionByIdAsync(Guid sessionId);
    Task<ProctoringSession?> GetSessionForStudentAsync(Guid sessionId, Guid studentId);
    Task<HashSet<Guid>> GetValidQuestionIdsAsync(IReadOnlyCollection<Guid> questionIds);
    Task<ProctoringViolationSummary?> GetViolationSummaryAsync(Guid sessionId);
    void AddProctoringSession(ProctoringSession session);
    void AddProctoringEvent(ProctoringEvent proctoringEvent);
    void AddProctoringEvents(IEnumerable<ProctoringEvent> proctoringEvents);
    void AddViolationSummary(ProctoringViolationSummary summary);
    bool IsViolationSummaryNew(ProctoringViolationSummary summary);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
