using Microsoft.EntityFrameworkCore;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.Proctor.Service.Managers;

public class ProctorManager : IProctorManager
{
    private readonly TaskverseContext _context;

    public ProctorManager(TaskverseContext context)
    {
        _context = context;
    }

    public async Task<Student?> GetStudentByUserIdAsync(Guid studentUserId)
        => await _context.Students.FirstOrDefaultAsync(item => item.UserId == studentUserId);

    public async Task<Attempt?> GetAttemptForStudentAsync(Guid attemptId, Guid studentId)
        => await _context.Attempts.FirstOrDefaultAsync(item => item.AttemptId == attemptId && item.StudentId == studentId);

    public async Task<ProctoringSession?> GetActiveSessionForAttemptAsync(Guid attemptId, Guid studentId)
        => await _context.ProctoringSessions
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(item =>
                item.AttemptId == attemptId &&
                item.StudentId == studentId &&
                item.ProctoringStatus == (int)ProctoringStatus.Active &&
                item.EndedAt == null);

    public async Task<List<ProctoringSession>> GetSessionsByAttemptAsync(Guid attemptId)
        => await _context.ProctoringSessions
            .Where(item => item.AttemptId == attemptId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync();

    public async Task<ProctoringSession?> GetSessionByIdAsync(Guid sessionId)
        => await _context.ProctoringSessions
            .FirstOrDefaultAsync(item => item.ProctoringSessionId == sessionId);

    public async Task<ProctoringSession?> GetSessionForStudentAsync(Guid sessionId, Guid studentId)
        => await _context.ProctoringSessions
            .FirstOrDefaultAsync(item => item.ProctoringSessionId == sessionId && item.StudentId == studentId);

    public async Task<HashSet<Guid>> GetValidQuestionIdsAsync(IReadOnlyCollection<Guid> questionIds)
    {
        if (questionIds.Count == 0)
        {
            return [];
        }

        var validQuestionIds = await _context.Questions
            .Where(item => questionIds.Contains(item.QuestionId))
            .Select(item => item.QuestionId)
            .ToListAsync();

        return validQuestionIds.ToHashSet();
    }

    public async Task<ProctoringViolationSummary?> GetViolationSummaryAsync(Guid sessionId)
        => await _context.ProctoringViolationSummaries
            .FirstOrDefaultAsync(item => item.ProctoringSessionId == sessionId);

    public void AddProctoringSession(ProctoringSession session)
        => _context.ProctoringSessions.Add(session);

    public void AddProctoringEvent(ProctoringEvent proctoringEvent)
        => _context.ProctoringEvents.Add(proctoringEvent);

    public void AddProctoringEvents(IEnumerable<ProctoringEvent> proctoringEvents)
        => _context.ProctoringEvents.AddRange(proctoringEvents);

    public void AddViolationSummary(ProctoringViolationSummary summary)
        => _context.ProctoringViolationSummaries.Add(summary);

    public bool IsViolationSummaryNew(ProctoringViolationSummary summary)
        => _context.Entry(summary).State == EntityState.Added;

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
