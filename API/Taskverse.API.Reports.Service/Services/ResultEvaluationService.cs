using Microsoft.EntityFrameworkCore;
using Taskverse.API.Reports.Service.Models;
using Taskverse.Business.Enums;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Reports.Service.Services;

public class ResultEvaluationService : IResultEvaluationService
{
    private const decimal PassingPercentage = 50m;

    private readonly TaskverseContext _context;
    private readonly IResultEvaluationStrategyFactory _strategyFactory;

    public ResultEvaluationService(
        TaskverseContext context,
        IResultEvaluationStrategyFactory strategyFactory)
    {
        _context = context;
        _strategyFactory = strategyFactory;
    }

    public async Task<AttemptResultResponse> EvaluateAttemptAsync(
        Guid attemptId,
        CancellationToken cancellationToken = default)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException("Attempt id is required.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        if (await _context.Results.AnyAsync(item => item.AttemptId == attemptId, cancellationToken))
        {
            throw new InvalidOperationException($"A result already exists for attempt '{attemptId}'.");
        }

        var attempt = await _context.Attempts
            .FirstOrDefaultAsync(item => item.AttemptId == attemptId, cancellationToken)
            ?? throw new KeyNotFoundException($"Attempt '{attemptId}' was not found.");

        if (attempt.AttemptStatus is not (AttemptStatus.Submitted or AttemptStatus.Auto_Submitted))
        {
            throw new InvalidOperationException("Only submitted attempts can be evaluated.");
        }

        var assessment = await _context.Assessments
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.AssessmentId == attempt.AssessmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Assessment '{attempt.AssessmentId}' was not found for the attempt.");

        var questionContexts = await (
            from assessmentQuestion in _context.AssessmentQuestions.AsNoTracking()
            join question in _context.Questions.AsNoTracking()
                on assessmentQuestion.QuestionId equals question.QuestionId
            where assessmentQuestion.AssessmentId == attempt.AssessmentId
            select new AssessmentQuestionEvaluationContext(
                question.QuestionId,
                question.QuestionType,
                question.Answer,
                question.Marks,
                question.NegativeMarks))
            .ToListAsync(cancellationToken);

        if (questionContexts.Count == 0)
        {
            throw new InvalidOperationException("No questions were found for this assessment attempt.");
        }

        var attemptAnswers = await _context.AttemptAnswers
            .Where(item => item.AttemptId == attempt.AttemptId)
            .ToListAsync(cancellationToken);

        var attemptAnswerByQuestionId = attemptAnswers.ToDictionary(item => item.QuestionId);

        var totalMarks = questionContexts.Sum(item => item.Marks);
        decimal obtainedMarks = 0;
        var correctAnswers = 0;
        var wrongAnswers = 0;
        var attemptedQuestions = 0;
        var hasCodingQuestions = false;

        foreach (var question in questionContexts)
        {
            attemptAnswerByQuestionId.TryGetValue(question.QuestionId, out var attemptAnswer);

            var strategy = _strategyFactory.Resolve(question.QuestionType);
            var evaluation = strategy.Evaluate(question, attemptAnswer);

            if (string.Equals(question.QuestionType, "coding", StringComparison.OrdinalIgnoreCase))
            {
                hasCodingQuestions = true;
            }

            if (evaluation.IsAnswered)
            {
                attemptedQuestions++;
            }

            if (evaluation.ShouldUpdateAttemptAnswer && attemptAnswer is not null)
            {
                attemptAnswer.IsCorrect = evaluation.IsCorrect;
                attemptAnswer.MarksAwarded = evaluation.AwardedMarks;
            }

            if (evaluation.IsPending)
            {
                continue;
            }

            obtainedMarks += evaluation.AwardedMarks;

            if (!evaluation.IsAnswered)
            {
                continue;
            }

            if (evaluation.IsCorrect)
            {
                correctAnswers++;
            }
            else
            {
                wrongAnswers++;
            }
        }

        var unansweredQuestions = Math.Max(0, questionContexts.Count - attemptedQuestions);
        var percentage = totalMarks <= 0
            ? 0
            : Math.Round((obtainedMarks / totalMarks) * 100m, 2, MidpointRounding.AwayFromZero);
        var resultStatus = hasCodingQuestions
            ? ResultStatus.Pending
            : percentage >= PassingPercentage ? ResultStatus.Pass : ResultStatus.Fail;

        attempt.CorrectAnswers = correctAnswers;
        attempt.WrongAnswers = wrongAnswers;
        attempt.AttemptedQuestions = attemptedQuestions;
        attempt.UnansweredQuestions = unansweredQuestions;
        attempt.TotalScore = obtainedMarks;
        attempt.Percentage = percentage;
        attempt.IsPassed = resultStatus == ResultStatus.Pass;

        var result = new Result
        {
            ResultId = Guid.NewGuid(),
            AssessmentId = attempt.AssessmentId,
            AttemptId = attempt.AttemptId,
            StudentId = attempt.StudentId,
            TotalMarks = totalMarks,
            ObtainedMarks = obtainedMarks,
            Percentage = percentage,
            Rank = 0,
            ResultStatus = resultStatus,
            GeneratedAt = DateTime.UtcNow
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync(cancellationToken);

        await RecalculateRanksAsync(assessment.AssessmentId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AttemptResultResponse(
            result.ResultId,
            result.AssessmentId,
            result.AttemptId,
            result.StudentId,
            result.TotalMarks,
            result.ObtainedMarks,
            result.Percentage,
            result.Rank,
            result.ResultStatus.ToString().ToUpperInvariant(),
            result.GeneratedAt,
            hasCodingQuestions);
    }

    private async Task RecalculateRanksAsync(Guid assessmentId, CancellationToken cancellationToken)
    {
        var rankedResults = await _context.Results
            .Where(item => item.AssessmentId == assessmentId)
            .OrderByDescending(item => item.ObtainedMarks)
            .ThenByDescending(item => item.Percentage)
            .ThenBy(item => item.GeneratedAt)
            .ThenBy(item => item.ResultId)
            .ToListAsync(cancellationToken);

        for (var index = 0; index < rankedResults.Count; index++)
        {
            rankedResults[index].Rank = index + 1;
        }
    }
}
