using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.Proctor.Service.Managers;

public class ProctorManager : IProctorManager
{
    private readonly TaskverseContext _context;
    private readonly ILogger<ProctorManager> _logger;

    public ProctorManager(TaskverseContext context, ILogger<ProctorManager> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Student?> GetStudentByUserIdAsync(Guid studentUserId)
        => await ExecuteQueryAsync(
            () => _context.Students.FirstOrDefaultAsync(item => item.UserId == studentUserId),
            $"retrieving the student profile for user '{studentUserId}'.");

    public async Task<Attempt?> GetAttemptForStudentAsync(Guid attemptId, Guid studentId)
        => await ExecuteQueryAsync(
            () => _context.Attempts.FirstOrDefaultAsync(item => item.AttemptId == attemptId && item.StudentId == studentId),
            $"retrieving attempt '{attemptId}' for student '{studentId}'.");

    public async Task<ProctoringSession?> GetActiveSessionForAttemptAsync(Guid attemptId, Guid studentId)
        => await ExecuteQueryAsync(
            () => _context.ProctoringSessions
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync(item =>
                    item.AttemptId == attemptId &&
                    item.StudentId == studentId &&
                    item.ProctoringStatus == (int)ProctoringStatus.Active &&
                    item.EndedAt == null),
            $"retrieving the active proctoring session for attempt '{attemptId}' and student '{studentId}'.");

    public async Task<List<ProctoringSession>> GetSessionsByAttemptAsync(Guid attemptId)
        => await ExecuteQueryAsync(
            () => _context.ProctoringSessions
                .Where(item => item.AttemptId == attemptId)
                .OrderByDescending(item => item.CreatedAt)
                .ToListAsync(),
            $"retrieving proctoring sessions for attempt '{attemptId}'.");

    public async Task<ProctoringSession?> GetSessionByIdAsync(Guid sessionId)
        => await ExecuteQueryAsync(
            () => _context.ProctoringSessions
                .FirstOrDefaultAsync(item => item.ProctoringSessionId == sessionId),
            $"retrieving proctoring session '{sessionId}'.");

    public async Task<ProctoringSession?> GetSessionForStudentAsync(Guid sessionId, Guid studentId)
        => await ExecuteQueryAsync(
            () => _context.ProctoringSessions
                .FirstOrDefaultAsync(item => item.ProctoringSessionId == sessionId && item.StudentId == studentId),
            $"retrieving proctoring session '{sessionId}' for student '{studentId}'.");

    public async Task<HashSet<Guid>> GetValidQuestionIdsAsync(IReadOnlyCollection<Guid> questionIds)
    {
        if (questionIds.Count == 0)
        {
            return [];
        }

        var validQuestionIds = await ExecuteQueryAsync(
            () => _context.Questions
                .Where(item => questionIds.Contains(item.QuestionId))
                .Select(item => item.QuestionId)
                .ToListAsync(),
            "retrieving valid question ids for the proctoring event batch.");

        return validQuestionIds.ToHashSet();
    }

    public async Task<ProctoringViolationSummary?> GetViolationSummaryAsync(Guid sessionId)
        => await ExecuteQueryAsync(
            () => _context.ProctoringViolationSummaries
                .FirstOrDefaultAsync(item => item.ProctoringSessionId == sessionId),
            $"retrieving the proctoring violation summary for session '{sessionId}'.");

    public void AddProctoringSession(ProctoringSession session)
        => ExecuteCommand(
            () => _context.ProctoringSessions.Add(session),
            $"staging proctoring session '{session.ProctoringSessionId}'.");

    public void AddProctoringEvent(ProctoringEvent proctoringEvent)
        => ExecuteCommand(
            () => _context.ProctoringEvents.Add(proctoringEvent),
            $"staging proctoring event '{proctoringEvent.EventType}' for session '{proctoringEvent.ProctoringSessionId}'.");

    public void AddProctoringEvents(IEnumerable<ProctoringEvent> proctoringEvents)
        => ExecuteCommand(
            () => _context.ProctoringEvents.AddRange(proctoringEvents),
            "staging a batch of proctoring events.");

    public void AddViolationSummary(ProctoringViolationSummary summary)
        => ExecuteCommand(
            () => _context.ProctoringViolationSummaries.Add(summary),
            $"staging the proctoring violation summary for session '{summary.ProctoringSessionId}'.");

    public bool IsViolationSummaryNew(ProctoringViolationSummary summary)
        => ExecuteQuery(
            () => _context.Entry(summary).State == EntityState.Added,
            $"checking whether the proctoring violation summary for session '{summary.ProctoringSessionId}' is new.");

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await ExecuteQueryAsync(
            () => _context.SaveChangesAsync(cancellationToken),
            "saving proctoring changes.");

    private async Task<T> ExecuteQueryAsync<T>(Func<Task<T>> operation, string operationDescription)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while {OperationDescription}", operationDescription);
            throw;
        }
    }

    private T ExecuteQuery<T>(Func<T> operation, string operationDescription)
    {
        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while {OperationDescription}", operationDescription);
            throw;
        }
    }

    private void ExecuteCommand(Action operation, string operationDescription)
    {
        try
        {
            operation();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while {OperationDescription}", operationDescription);
            throw;
        }
    }
}
