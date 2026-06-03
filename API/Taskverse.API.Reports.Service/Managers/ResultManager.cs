using Microsoft.EntityFrameworkCore;
using Npgsql;
using Taskverse.API.Reports.Service.Models;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.Reports.Service.Managers;

public class ResultManager : IResultManager
{
    private static readonly AttemptStatus[] SubmittedAttemptStatuses =
    [
        AttemptStatus.Submitted,
        AttemptStatus.Auto_Submitted
    ];

    private readonly TaskverseContext _context;

    public ResultManager(TaskverseContext context)
    {
        _context = context;
    }

    public Task<bool> ResultExistsForAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        return _context.Results.AnyAsync(item => item.AttemptId == attemptId, cancellationToken);
    }

    public Task<Attempt?> GetAttemptAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        return _context.Attempts
            .FirstOrDefaultAsync(item => item.AttemptId == attemptId, cancellationToken);
    }

    public Task<Assessment?> GetAssessmentAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        return _context.Assessments
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.AssessmentId == assessmentId, cancellationToken);
    }

    public Task<List<AttemptAnswer>> GetAttemptAnswersAsync(Guid attemptId, CancellationToken cancellationToken = default)
    {
        return _context.AttemptAnswers
            .Where(item => item.AttemptId == attemptId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SubmittedAttemptScoreSnapshot>> GetSubmittedAttemptScoreSnapshotsAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from attempt in _context.Attempts.AsNoTracking()
            where attempt.AssessmentId == assessmentId &&
                  SubmittedAttemptStatuses.Contains(attempt.AttemptStatus)
            join attemptAnswer in _context.AttemptAnswers.AsNoTracking()
                on attempt.AttemptId equals attemptAnswer.AttemptId into attemptAnswerGroup
            select new SubmittedAttemptScoreSnapshot(
                attempt.AttemptId,
                attemptAnswerGroup.Sum(item => (decimal?)item.MarksAwarded) ?? 0m))
            .ToListAsync(cancellationToken);
    }

    public async Task PersistAttemptEvaluationAsync(
        Attempt attempt,
        Result result,
        IReadOnlyDictionary<Guid, int> rankByAttemptId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _context.Results.Add(result);

            var existingResults = await _context.Results
                .Where(item => item.AssessmentId == result.AssessmentId)
                .ToListAsync(cancellationToken);

            foreach (var existingResult in existingResults)
            {
                if (rankByAttemptId.TryGetValue(existingResult.AttemptId, out var rank))
                {
                    existingResult.Rank = rank;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateAttemptResult(ex))
        {
            throw new InvalidOperationException(
                $"A result already exists for attempt '{result.AttemptId}'.",
                ex);
        }
    }

    public async Task<List<StudentResultResponse>> GetStudentResultsAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student id is required.");
        }

        var studentResults = await (
            from result in _context.Results.AsNoTracking()
            join assessment in _context.Assessments.AsNoTracking()
                on result.AssessmentId equals assessment.AssessmentId
            where result.StudentId == studentId && assessment.ShowResultsImmediately
            orderby result.GeneratedAt descending, result.ResultId descending
            select new
            {
                Result = result,
                assessment.AssessmentName
            })
            .ToListAsync(cancellationToken);

        return studentResults
            .Select(item => item.Result.ToStudentResultResponse(
                item.AssessmentName,
                item.Result.ResultStatus == ResultStatus.Pending))
            .ToList();
    }

    private static bool IsDuplicateAttemptResult(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException &&
               postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
               string.Equals(postgresException.ConstraintName, "IX_results_attempt_id", StringComparison.Ordinal);
    }
}
