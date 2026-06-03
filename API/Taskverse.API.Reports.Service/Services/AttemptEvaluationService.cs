using Taskverse.API.Reports.Service.Managers;
using Taskverse.API.Reports.Service.Models;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Enums;

namespace Taskverse.API.Reports.Service.Services;

public class AttemptEvaluationService : IAttemptEvaluationService
{
    private static readonly AttemptStatus[] SubmittedAttemptStatuses =
    [
        AttemptStatus.Submitted,
        AttemptStatus.Auto_Submitted
    ];

    private readonly IResultManager _resultManager;

    public AttemptEvaluationService(IResultManager resultManager)
    {
        _resultManager = resultManager;
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

        if (await _resultManager.ResultExistsForAttemptAsync(attemptId, cancellationToken))
        {
            throw new InvalidOperationException($"A result already exists for attempt '{attemptId}'.");
        }

        var attempt = await _resultManager.GetAttemptAsync(attemptId, cancellationToken)
            ?? throw new KeyNotFoundException($"Attempt '{attemptId}' was not found.");

        if (!SubmittedAttemptStatuses.Contains(attempt.AttemptStatus))
        {
            throw new InvalidOperationException("Only submitted attempts can be evaluated.");
        }

        var assessment = await _resultManager.GetAssessmentAsync(attempt.AssessmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Assessment '{attempt.AssessmentId}' was not found for the attempt.");

        if (assessment.AssessmentType is not AssessmentType.Objective)
        {
            return AttemptEvaluationExecutionResult.Skipped();
        }

        var attemptAnswers = await _resultManager.GetAttemptAnswersAsync(attempt.AttemptId, cancellationToken);
        var evaluation = BuildEvaluation(attempt, assessment, attemptAnswers, passingPercentage);
        var rankingSnapshots = await _resultManager.GetSubmittedAttemptScoreSnapshotsAsync(
            assessment.AssessmentId,
            cancellationToken);

        var rankByAttemptId = CalculateCompetitionRanks(rankingSnapshots, attempt.AttemptId, evaluation.ObtainedMarks);
        var rank = rankByAttemptId.TryGetValue(attempt.AttemptId, out var computedRank)
            ? computedRank
            : 1;

        var result = new Result
        {
            ResultId = Guid.NewGuid(),
            AssessmentId = attempt.AssessmentId,
            AttemptId = attempt.AttemptId,
            StudentId = attempt.StudentId,
            TotalMarks = assessment.TotalMarks,
            ObtainedMarks = evaluation.ObtainedMarks,
            Percentage = evaluation.Percentage,
            Rank = rank,
            ResultStatus = evaluation.ResultStatus,
            GeneratedAt = DateTime.UtcNow
        };

        await _resultManager.PersistAttemptEvaluationAsync(attempt, result, rankByAttemptId, cancellationToken);
        return AttemptEvaluationExecutionResult.Completed(result.ToAttemptResultResponse(hasPendingCodingEvaluation: false));
    }

    private static AttemptEvaluationSummary BuildEvaluation(
        Attempt attempt,
        Assessment assessment,
        IReadOnlyCollection<AttemptAnswer> attemptAnswers,
        int passingPercentage)
    {
        var answeredAttemptAnswers = attemptAnswers
            .Where(item => !string.IsNullOrWhiteSpace(item.SelectedAnswer))
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
        var resultStatus = percentage >= passingPercentage
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
            resultStatus);
    }

    private static Dictionary<Guid, int> CalculateCompetitionRanks(
        IEnumerable<SubmittedAttemptScoreSnapshot> persistedSnapshots,
        Guid currentAttemptId,
        decimal currentAttemptMarks)
    {
        var snapshots = persistedSnapshots.ToList();
        var currentSnapshot = snapshots.FirstOrDefault(item => item.AttemptId == currentAttemptId);
        if (currentSnapshot is null)
        {
            snapshots.Add(new SubmittedAttemptScoreSnapshot(currentAttemptId, currentAttemptMarks));
        }
        else
        {
            var index = snapshots.FindIndex(item => item.AttemptId == currentAttemptId);
            snapshots[index] = currentSnapshot with { ObtainedMarks = currentAttemptMarks };
        }

        var orderedSnapshots = snapshots
            .OrderByDescending(item => item.ObtainedMarks)
            .ThenBy(item => item.AttemptId)
            .ToList();

        var rankByAttemptId = new Dictionary<Guid, int>(orderedSnapshots.Count);
        decimal? previousMarks = null;
        var previousRank = 0;

        for (var index = 0; index < orderedSnapshots.Count; index++)
        {
            var snapshot = orderedSnapshots[index];
            var rank = previousMarks.HasValue && snapshot.ObtainedMarks == previousMarks.Value
                ? previousRank
                : index + 1;

            rankByAttemptId[snapshot.AttemptId] = rank;
            previousMarks = snapshot.ObtainedMarks;
            previousRank = rank;
        }

        return rankByAttemptId;
    }

    private static void ValidatePassingPercentage(int passingPercentage)
    {
        if (passingPercentage is < 0 or > 100)
        {
            throw new ArgumentException("Passing percentage must be between 0 and 100.");
        }
    }
}
