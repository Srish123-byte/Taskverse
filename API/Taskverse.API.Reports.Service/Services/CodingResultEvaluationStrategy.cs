using Taskverse.API.Reports.Service.Models;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Reports.Service.Services;

public class CodingResultEvaluationStrategy : IResultEvaluationStrategy
{
    public bool CanHandle(string questionType)
        => string.Equals(questionType?.Trim(), "coding", StringComparison.OrdinalIgnoreCase);

    public QuestionEvaluationResult Evaluate(
        AssessmentQuestionEvaluationContext question,
        AttemptAnswer? attemptAnswer)
    {
        var hasAnswer = !string.IsNullOrWhiteSpace(attemptAnswer?.SelectedAnswer);

        return new QuestionEvaluationResult(
            IsPending: true,
            IsAnswered: hasAnswer,
            IsCorrect: false,
            AwardedMarks: 0,
            ShouldUpdateAttemptAnswer: false);
    }
}
