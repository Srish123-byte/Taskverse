using Taskverse.API.Reports.Service.Models;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Reports.Service.Services;

public class McqResultEvaluationStrategy : IResultEvaluationStrategy
{
    public bool CanHandle(string questionType)
        => string.Equals(questionType?.Trim(), "mcq", StringComparison.OrdinalIgnoreCase);

    public QuestionEvaluationResult Evaluate(
        AssessmentQuestionEvaluationContext question,
        AttemptAnswer? attemptAnswer)
    {
        var hasAnswer = !string.IsNullOrWhiteSpace(attemptAnswer?.SelectedAnswer);
        if (!hasAnswer)
        {
            return new QuestionEvaluationResult(
                IsPending: false,
                IsAnswered: false,
                IsCorrect: false,
                AwardedMarks: 0,
                ShouldUpdateAttemptAnswer: attemptAnswer is not null);
        }

        var isCorrect = string.Equals(
            attemptAnswer!.SelectedAnswer,
            question.CorrectAnswer,
            StringComparison.Ordinal);

        var awardedMarks = isCorrect
            ? question.Marks
            : question.NegativeMarks > 0 ? -question.NegativeMarks : 0;

        return new QuestionEvaluationResult(
            IsPending: false,
            IsAnswered: true,
            IsCorrect: isCorrect,
            AwardedMarks: awardedMarks,
            ShouldUpdateAttemptAnswer: true);
    }
}
