using Taskverse.API.Reports.Service.Models;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Utilities;

namespace Taskverse.API.Reports.Service.Services;

public class McqResultEvaluationStrategy : IResultEvaluationStrategy
{
    public bool CanHandle(string questionType)
        => string.Equals(questionType?.Trim(), "mcq", StringComparison.OrdinalIgnoreCase)
           || string.Equals(questionType?.Trim(), "fill in the blanks", StringComparison.OrdinalIgnoreCase);

    public QuestionEvaluationResult Evaluate(
        AssessmentQuestionEvaluationContext question,
        AttemptAnswer? attemptAnswer)
    {
        var selectedAnswers = QuestionAnswerJsonHelper.ParseStoredAnswers(attemptAnswer?.SelectedAnswer);
        var correctAnswers = QuestionAnswerJsonHelper.ParseStoredAnswers(question.CorrectAnswer);
        var hasAnswer = selectedAnswers.Count > 0;
        if (!hasAnswer)
        {
            return new QuestionEvaluationResult(
                IsPending: false,
                IsAnswered: false,
                IsCorrect: false,
                AwardedMarks: 0,
                ShouldUpdateAttemptAnswer: attemptAnswer is not null);
        }

        var isMultiCorrect = correctAnswers.Count > 1;
        var isCorrect = false;
        decimal awardedMarks;

        if (isMultiCorrect)
        {
            var correctAnswerLookup = new HashSet<string>(correctAnswers, StringComparer.OrdinalIgnoreCase);
            var hasWrongSelection = selectedAnswers.Any(answer => !correctAnswerLookup.Contains(answer));
            var selectedCorrectCount = selectedAnswers.Count(answer => correctAnswerLookup.Contains(answer));
            var perChoiceMarks = correctAnswers.Count == 0
                ? 0
                : question.Marks / correctAnswers.Count;

            isCorrect = !hasWrongSelection && selectedCorrectCount == correctAnswers.Count;
            awardedMarks = hasWrongSelection
                ? 0
                : perChoiceMarks * selectedCorrectCount;
        }
        else
        {
            var selectedAnswer = selectedAnswers.FirstOrDefault();
            var correctAnswer = correctAnswers.FirstOrDefault();
            isCorrect = string.Equals(selectedAnswer, correctAnswer, StringComparison.OrdinalIgnoreCase);
            awardedMarks = isCorrect
                ? question.Marks
                : question.NegativeMarks > 0 ? -question.NegativeMarks : 0;
        }

        return new QuestionEvaluationResult(
            IsPending: false,
            IsAnswered: true,
            IsCorrect: isCorrect,
            AwardedMarks: awardedMarks,
            ShouldUpdateAttemptAnswer: true);
    }
}
