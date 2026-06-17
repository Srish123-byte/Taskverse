using Taskverse.API.Reports.Service.Managers;
using Taskverse.API.Reports.Service.Models;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;
using Taskverse.Data.Utilities;

namespace Taskverse.API.Reports.Service.Services;

public class AttemptEvaluationService : IAttemptEvaluationService
{
    private static readonly AttemptStatus[] SubmittedAttemptStatuses =
    [
        AttemptStatus.Submitted,
        AttemptStatus.Auto_Submitted
    ];

    private readonly IResultManager _resultManager;
    private readonly IResultEvaluationStrategyFactory _resultEvaluationStrategyFactory;
    private readonly ILogger<AttemptEvaluationService> _logger;

    public AttemptEvaluationService(
        IResultManager resultManager,
        IResultEvaluationStrategyFactory resultEvaluationStrategyFactory,
        ILogger<AttemptEvaluationService> logger)
    {
        _resultManager = resultManager;
        _resultEvaluationStrategyFactory = resultEvaluationStrategyFactory;
        _logger = logger;
    }

    public async Task<AttemptEvaluationExecutionResult> EvaluateAttemptAsync(
        Guid attemptId,
        int passingPercentage,
        CancellationToken cancellationToken = default)
    {
        if (attemptId == Guid.Empty)
        {
            throw new ArgumentException("Attempt id is required.");
        }

        ValidatePassingPercentage(passingPercentage);
        _logger.LogInformation(
            "Starting result evaluation for attemptId={AttemptId} with passingPercentage={PassingPercentage}.",
            attemptId,
            passingPercentage);

        if (await _resultManager.ResultExistsForAttemptAsync(attemptId, cancellationToken))
        {
            _logger.LogWarning("Skipping result evaluation because a result already exists for attemptId={AttemptId}.", attemptId);
            return AttemptEvaluationExecutionResult.Skipped();
        }

        var attempt = await _resultManager.GetAttemptAsync(attemptId, cancellationToken)
            ?? throw new KeyNotFoundException($"Attempt '{attemptId}' was not found.");
        _logger.LogInformation(
            "Loaded attempt for evaluation. attemptId={AttemptId}, assessmentId={AssessmentId}, status={AttemptStatus}.",
            attempt.AttemptId,
            attempt.AssessmentId,
            attempt.AttemptStatus);

        if (!SubmittedAttemptStatuses.Contains(attempt.AttemptStatus))
        {
            throw new InvalidOperationException("Only submitted attempts can be evaluated.");
        }

        var assessment = await _resultManager.GetAssessmentAsync(attempt.AssessmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Assessment '{attempt.AssessmentId}' was not found for the attempt.");
        _logger.LogInformation(
            "Loaded assessment for evaluation. assessmentId={AssessmentId}, totalMarks={TotalMarks}, type={AssessmentType}.",
            assessment.AssessmentId,
            assessment.TotalMarks,
            assessment.AssessmentType);

        var attemptAnswers = await _resultManager.GetAttemptAnswersAsync(attempt.AttemptId, cancellationToken);
        var assessmentQuestionContexts = await _resultManager.GetAssessmentQuestionEvaluationContextsAsync(
            assessment.AssessmentId,
            cancellationToken);
        _logger.LogInformation(
            "Loaded evaluation inputs for attemptId={AttemptId}. attemptAnswers={AttemptAnswersCount}, questions={QuestionCount}.",
            attempt.AttemptId,
            attemptAnswers.Count,
            assessmentQuestionContexts.Count);

        var evaluation = BuildEvaluation(
            attempt,
            assessment,
            attemptAnswers,
            assessmentQuestionContexts,
            passingPercentage,
            _resultEvaluationStrategyFactory);

        // Rank is computed inside PersistAttemptEvaluationAsync based on the
        // results table — so we don't need to pre-calculate it here.
        var result = new Result
        {
            ResultId = Guid.NewGuid(),
            AssessmentId = attempt.AssessmentId,
            AttemptId = attempt.AttemptId,
            StudentId = attempt.StudentId,
            TotalMarks = assessment.TotalMarks,
            ObtainedMarks = attempt.TotalScore,
            Percentage = attempt.Percentage,
            Rank = 1,   // default; overwritten by PersistAttemptEvaluationAsync
            ResultStatus = evaluation.ResultStatus,
            GeneratedAt = DateTime.UtcNow
        };
        _logger.LogInformation(
            "Computed evaluation for attemptId={AttemptId}. totalScore={TotalScore}, percentage={Percentage}, status={ResultStatus}, pendingCoding={HasPendingCodingEvaluation}.",
            attempt.AttemptId,
            attempt.TotalScore,
            attempt.Percentage,
            result.ResultStatus,
            evaluation.HasPendingCodingEvaluation);

        await _resultManager.PersistAttemptEvaluationAsync(attempt, result, cancellationToken);
        _logger.LogInformation(
            "Persisted evaluation for attemptId={AttemptId}. resultId={ResultId}, rank={Rank}.",
            attempt.AttemptId,
            result.ResultId,
            result.Rank);

        return AttemptEvaluationExecutionResult.Completed(result.ToAttemptResultResponse(evaluation.HasPendingCodingEvaluation));
    }

    private static AttemptEvaluationSummary BuildEvaluation(
        Attempt attempt,
        Assessment assessment,
        IReadOnlyCollection<AttemptAnswer> attemptAnswers,
        IReadOnlyCollection<AssessmentQuestionEvaluationContext> assessmentQuestionContexts,
        int passingPercentage,
        IResultEvaluationStrategyFactory resultEvaluationStrategyFactory)
    {
        var attemptAnswerByQuestionId = attemptAnswers.ToDictionary(item => item.QuestionId, item => item);
        var hasPendingCodingEvaluation = false;

        foreach (var questionContext in assessmentQuestionContexts)
        {
            var strategy = resultEvaluationStrategyFactory.Resolve(questionContext.QuestionType);
            attemptAnswerByQuestionId.TryGetValue(questionContext.QuestionId, out var attemptAnswer);

            var questionEvaluation = strategy.Evaluate(questionContext, attemptAnswer);
            if (questionEvaluation.IsPending)
            {
                hasPendingCodingEvaluation = true;
            }

            if (questionEvaluation.ShouldUpdateAttemptAnswer && attemptAnswer is not null)
            {
                attemptAnswer.IsCorrect = questionEvaluation.IsCorrect;
                attemptAnswer.MarksAwarded = questionEvaluation.AwardedMarks;
            }
        }

        var answeredAttemptAnswers = attemptAnswers
            .Where(IsAttemptAnswerAnswered)
            .ToList();
        var attemptedQuestions = answeredAttemptAnswers.Count;
        var correctAnswers = answeredAttemptAnswers.Count(item => item.IsCorrect);
        var wrongAnswers = answeredAttemptAnswers.Count(item => !item.IsCorrect);
        var unansweredQuestions = Math.Max(0, attempt.TotalQuestions - attemptedQuestions);
        var obtainedMarks = attemptAnswers.Sum(item => item.MarksAwarded);
        var totalMarks = Math.Max(0, assessment.TotalMarks);
        var percentage = totalMarks == 0
            ? 0
            : Math.Round((obtainedMarks / totalMarks) * 100m, 2, MidpointRounding.AwayFromZero);
        var resultStatus = hasPendingCodingEvaluation
            ? ResultStatus.Pending
            : percentage >= passingPercentage
                ? ResultStatus.Pass
                : ResultStatus.Fail;

        attempt.CorrectAnswers = correctAnswers;
        attempt.WrongAnswers = wrongAnswers;
        attempt.AttemptedQuestions = attemptedQuestions;
        attempt.UnansweredQuestions = unansweredQuestions;
        attempt.TotalScore = obtainedMarks;
        attempt.Percentage = percentage;
        attempt.IsPassed = resultStatus == ResultStatus.Pass;

        return new AttemptEvaluationSummary(
            obtainedMarks,
            percentage,
            resultStatus,
            hasPendingCodingEvaluation);
    }

    private static bool IsAttemptAnswerAnswered(AttemptAnswer attemptAnswer)
    {
        if (string.IsNullOrWhiteSpace(attemptAnswer.SelectedAnswer))
        {
            return false;
        }

        return QuestionAnswerJsonHelper.ParseStoredAnswers(attemptAnswer.SelectedAnswer).Count > 0;
    }

    private static void ValidatePassingPercentage(int passingPercentage)
    {
        if (passingPercentage is < 0 or > 100)
        {
            throw new ArgumentException("Passing percentage must be between 0 and 100.");
        }
    }
}
