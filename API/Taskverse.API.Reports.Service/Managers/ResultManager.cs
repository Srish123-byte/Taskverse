using Microsoft.EntityFrameworkCore;
using Npgsql;
using Taskverse.API.Reports.Service.Models;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;
using Taskverse.Data.Utilities;

namespace Taskverse.API.Reports.Service.Managers;

public class ResultManager : IResultManager
{
    private readonly TaskverseContext _context;
    private readonly ILogger<ResultManager> _logger;

    public ResultManager(TaskverseContext context, ILogger<ResultManager> logger)
    {
        _context = context;
        _logger = logger;
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

    public Task<List<AssessmentQuestionEvaluationContext>> GetAssessmentQuestionEvaluationContextsAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default)
    {
        return (
            from assessmentQuestion in _context.AssessmentQuestions.AsNoTracking()
            join question in _context.Questions.AsNoTracking()
                on assessmentQuestion.QuestionId equals question.QuestionId
            where assessmentQuestion.AssessmentId == assessmentId
            orderby assessmentQuestion.DisplayOrder
            select new AssessmentQuestionEvaluationContext(
                question.QuestionId,
                question.QuestionType,
                question.Answer,
                question.Marks,
                question.NegativeMarks))
            .ToListAsync(cancellationToken);
    }

    public async Task PersistAttemptEvaluationAsync(
        Attempt attempt,
        Result result,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _logger.LogInformation(
                "Persisting attempt evaluation. attemptId={AttemptId}, assessmentId={AssessmentId}, resultId={ResultId}, " +
                "totalScore={TotalScore}, percentage={Percentage}, correctAnswers={CorrectAnswers}, wrongAnswers={WrongAnswers}.",
                attempt.AttemptId,
                result.AssessmentId,
                result.ResultId,
                attempt.TotalScore,
                attempt.Percentage,
                attempt.CorrectAnswers,
                attempt.WrongAnswers);

            _context.Attempts.Update(attempt);

            // Load all results already persisted for this assessment so we can
            // re-rank everyone (including the student being evaluated right now).
            var existingResults = await _context.Results
                .Where(item => item.AssessmentId == result.AssessmentId)
                .ToListAsync(cancellationToken);

            _logger.LogDebug(
                "Found {ExistingResultCount} existing result(s) for assessmentId={AssessmentId}. Computing ranks.",
                existingResults.Count,
                result.AssessmentId);

            // Build the full leaderboard: existing results + the new one.
            // The first student to complete will have an empty existingResults list
            // and naturally receives rank 1.
            var allResultsForRanking = existingResults
                .Select(r => (r.AttemptId, r.ObtainedMarks))
                .Append((result.AttemptId, result.ObtainedMarks))
                .ToList();

            var rankByAttemptId = ComputeRanks(allResultsForRanking);

            foreach (var existingResult in existingResults)
            {
                existingResult.Rank = rankByAttemptId[existingResult.AttemptId];
            }

            result.Rank = rankByAttemptId[result.AttemptId];
            _context.Results.Add(result);

            var trackedEntries = _context.ChangeTracker.Entries()
                .Select(e => $"{e.Entity.GetType().Name}[{e.State}]")
                .ToList();
            _logger.LogDebug(
                "EF change tracker before SaveChanges for attemptId={AttemptId}: [{TrackedEntries}].",
                attempt.AttemptId,
                string.Join(", ", trackedEntries));

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation(
                "Persisted attempt evaluation successfully. attemptId={AttemptId}, resultId={ResultId}, rank={Rank}.",
                attempt.AttemptId,
                result.ResultId,
                result.Rank);
        }
        catch (DbUpdateException ex) when (IsDuplicateAttemptResult(ex))
        {
            throw new InvalidOperationException(
                $"A result already exists for attempt '{result.AttemptId}'.",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to persist attempt evaluation. attemptId={AttemptId}, resultId={ResultId}.",
                attempt.AttemptId,
                result.ResultId);
            throw;
        }
    }

    /// <summary>
    /// Assigns integer ranks to a set of (AttemptId, ObtainedMarks) pairs.
    /// Higher marks = lower rank number. Ties share the same rank.
    /// The first student (only one entry) always comes out as rank 1.
    /// </summary>
    private static Dictionary<Guid, int> ComputeRanks(
        IReadOnlyList<(Guid AttemptId, decimal ObtainedMarks)> entries)
    {
        var ordered = entries
            .OrderByDescending(e => e.ObtainedMarks)
            .ThenBy(e => e.AttemptId)   // deterministic tie-break
            .ToList();

        var rankByAttemptId = new Dictionary<Guid, int>(ordered.Count);
        decimal? previousMarks = null;
        var previousRank = 0;

        for (var i = 0; i < ordered.Count; i++)
        {
            var (attemptId, marks) = ordered[i];
            var rank = previousMarks.HasValue && marks == previousMarks.Value
                ? previousRank
                : i + 1;

            rankByAttemptId[attemptId] = rank;
            previousMarks = marks;
            previousRank = rank;
        }

        return rankByAttemptId;
    }

    public async Task<List<StudentResultResponse>> GetStudentResultsAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student id is required.");
        }

        var resolvedStudentId = await ResolveStudentProfileIdAsync(studentId, cancellationToken);

        var studentResults = await (
            from result in _context.Results.AsNoTracking()
            join assessment in _context.Assessments.AsNoTracking()
                on result.AssessmentId equals assessment.AssessmentId
            where result.StudentId == resolvedStudentId && assessment.ShowResultsImmediately
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
                submittedAt: null,
                durationMinutes: 0,
                totalQuestions: 0,
                attemptedQuestions: 0,
                correctAnswers: 0,
                wrongAnswers: 0,
                unansweredQuestions: 0,
                participantCount: 0,
                hasPendingCodingEvaluation: item.Result.ResultStatus == ResultStatus.Pending,
                showResultsImmediately: true))
            .ToList();
    }

    public async Task<StudentResultResponse> GetStudentAttemptResultAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException("Attempt id is required.");
        }

        var studentAttemptResult = await (
            from result in _context.Results.AsNoTracking()
            join attempt in _context.Attempts.AsNoTracking()
                on result.AttemptId equals attempt.AttemptId
            join assessment in _context.Assessments.AsNoTracking()
                on result.AssessmentId equals assessment.AssessmentId
            where result.AttemptId == attemptId
            select new
            {
                Result = result,
                Attempt = attempt,
                assessment.AssessmentName,
                assessment.DurationMinutes,
                assessment.ShowResultsImmediately
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (studentAttemptResult is null)
        {
            throw new KeyNotFoundException($"Result was not found for attempt '{attemptId}'.");
        }

        // When the instructor has not enabled immediate results, return a minimal
        // stub — the Angular will display a "You'll be notified" panel instead
        // of the full score / question-analysis view.
        if (!studentAttemptResult.ShowResultsImmediately)
        {
            return studentAttemptResult.Result.ToStudentResultResponse(
                studentAttemptResult.AssessmentName,
                submittedAt: null,
                durationMinutes: 0,
                totalQuestions: 0,
                attemptedQuestions: 0,
                correctAnswers: 0,
                wrongAnswers: 0,
                unansweredQuestions: 0,
                participantCount: 0,
                hasPendingCodingEvaluation: false,
                showResultsImmediately: false);
        }

        var participantCount = await _context.Results
            .AsNoTracking()
            .Where(item => item.AssessmentId == studentAttemptResult.Result.AssessmentId)
            .CountAsync(cancellationToken);

        var questionResults = await (
            from assessmentQuestion in _context.AssessmentQuestions.AsNoTracking()
            join question in _context.Questions.AsNoTracking()
                on assessmentQuestion.QuestionId equals question.QuestionId
            join attemptAnswer in _context.AttemptAnswers.AsNoTracking().Where(item => item.AttemptId == attemptId)
                on question.QuestionId equals attemptAnswer.QuestionId into attemptAnswerGroup
            from attemptAnswer in attemptAnswerGroup.DefaultIfEmpty()
            where assessmentQuestion.AssessmentId == studentAttemptResult.Result.AssessmentId
            orderby assessmentQuestion.DisplayOrder
            select new
            {
                question.QuestionId,
                assessmentQuestion.DisplayOrder,
                question.QuestionType,
                question.QuestionText,
                question.Marks,
                question.Answer,
                question.Explanation,
                SelectedAnswer = attemptAnswer != null ? attemptAnswer.SelectedAnswer : null,
                AwardedMarks = attemptAnswer != null ? attemptAnswer.MarksAwarded : 0m,
                IsCorrect = attemptAnswer != null ? (bool?)attemptAnswer.IsCorrect : null
            })
            .ToListAsync(cancellationToken);

        var mappedQuestionResults = questionResults
            .Select(item =>
            {
                var userAnswers = QuestionAnswerJsonHelper.ParseStoredAnswers(item.SelectedAnswer);
                var correctAnswers = QuestionAnswerJsonHelper.ParseStoredAnswers(item.Answer);
                var hasAnswered = userAnswers.Count > 0;
                var status = !hasAnswered
                    ? "UNANSWERED"
                    : string.Equals(item.QuestionType?.Trim(), "coding", StringComparison.OrdinalIgnoreCase) &&
                      studentAttemptResult.Result.ResultStatus == ResultStatus.Pending
                        ? "PENDING"
                        : item.IsCorrect == true
                            ? "CORRECT"
                            : "INCORRECT";

                return new StudentResultQuestionResultResponse(
                    item.QuestionId,
                    item.DisplayOrder,
                    item.QuestionType ?? string.Empty,
                    item.QuestionText,
                    item.Marks,
                    item.AwardedMarks,
                    status,
                    userAnswers,
                    correctAnswers,
                    item.Explanation);
            })
            .ToList();

        var questionExplanations = await (
            from assessmentQuestion in _context.AssessmentQuestions.AsNoTracking()
            join question in _context.Questions.AsNoTracking()
                on assessmentQuestion.QuestionId equals question.QuestionId
            where assessmentQuestion.AssessmentId == studentAttemptResult.Result.AssessmentId
            orderby assessmentQuestion.DisplayOrder
            select new StudentResultQuestionExplanationResponse(
                question.QuestionId,
                assessmentQuestion.DisplayOrder,
                question.QuestionType,
                question.QuestionText,
                question.Explanation))
            .ToListAsync(cancellationToken);

        return studentAttemptResult.Result.ToStudentResultResponse(
            studentAttemptResult.AssessmentName,
            studentAttemptResult.Attempt.SubmittedAt,
            studentAttemptResult.DurationMinutes,
            studentAttemptResult.Attempt.TotalQuestions,
            studentAttemptResult.Attempt.AttemptedQuestions,
            studentAttemptResult.Attempt.CorrectAnswers,
            studentAttemptResult.Attempt.WrongAnswers,
            studentAttemptResult.Attempt.UnansweredQuestions,
            participantCount,
            hasPendingCodingEvaluation: studentAttemptResult.Result.ResultStatus == ResultStatus.Pending,
            showResultsImmediately: true,
            mappedQuestionResults,
            questionExplanations);
    }

    private async Task<Guid> ResolveStudentProfileIdAsync(Guid studentIdentifier, CancellationToken cancellationToken)
    {
        var studentProfileId = await _context.Students
            .AsNoTracking()
            .Where(item => item.UserId == studentIdentifier)
            .Select(item => (Guid?)item.StudentId)
            .FirstOrDefaultAsync(cancellationToken);

        return studentProfileId ?? studentIdentifier;
    }

    private static bool IsDuplicateAttemptResult(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException &&
               postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
               string.Equals(postgresException.ConstraintName, "IX_results_attempt_id", StringComparison.Ordinal);
    }
}
