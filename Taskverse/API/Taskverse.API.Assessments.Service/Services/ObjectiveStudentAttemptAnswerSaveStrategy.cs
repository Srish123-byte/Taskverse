using Microsoft.EntityFrameworkCore;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Utilities;

namespace Taskverse.API.Assessments.Service.Services;

public class ObjectiveStudentAttemptAnswerSaveStrategy : IStudentAttemptAnswerSaveStrategy
{
    public bool CanHandle(string questionType)
        => !string.Equals(questionType?.Trim(), "coding", StringComparison.OrdinalIgnoreCase);

    public async Task<AttemptAnswer> SaveAsync(
        TaskverseContext context,
        Attempt attempt,
        Assessment assessment,
        Question question,
        SaveStudentAttemptAnswerRequest request,
        DateTime answeredAtUtc,
        CancellationToken cancellationToken = default)
    {
        var existingAnswer = await context.AttemptAnswers
            .FirstOrDefaultAsync(
                item => item.AttemptId == attempt.AttemptId && item.QuestionId == question.QuestionId,
                cancellationToken);

        if (existingAnswer is null)
        {
            existingAnswer = new AttemptAnswer
            {
                AttemptAnswerId = Guid.NewGuid(),
                AttemptId = attempt.AttemptId,
                QuestionId = question.QuestionId
            };

            context.AttemptAnswers.Add(existingAnswer);
        }

        var normalizedSelectedAnswers = request.SelectedAnswers?.Count > 0
            ? QuestionAnswerJsonHelper.NormalizeAnswerValues(request.SelectedAnswers)
            : QuestionAnswerJsonHelper.ParseStoredAnswers(request.SelectedAnswer);
        var normalizedCorrectAnswers = QuestionAnswerJsonHelper.ParseStoredAnswers(question.Answer);
        var hasAnswered = normalizedSelectedAnswers.Count > 0;
        var isMultiCorrectMcq = string.Equals(question.QuestionType?.Trim(), "mcq", StringComparison.OrdinalIgnoreCase) &&
                                normalizedCorrectAnswers.Count > 1;
        var isCorrect = false;
        decimal marksAwarded;

        if (isMultiCorrectMcq)
        {
            var correctAnswerLookup = new HashSet<string>(normalizedCorrectAnswers, StringComparer.OrdinalIgnoreCase);
            var hasWrongSelection = normalizedSelectedAnswers.Any(answer => !correctAnswerLookup.Contains(answer));
            var selectedCorrectCount = normalizedSelectedAnswers.Count(answer => correctAnswerLookup.Contains(answer));
            var perChoiceMarks = normalizedCorrectAnswers.Count == 0
                ? 0
                : question.Marks / normalizedCorrectAnswers.Count;

            isCorrect = hasAnswered &&
                        !hasWrongSelection &&
                        selectedCorrectCount == normalizedCorrectAnswers.Count;
            marksAwarded = !hasAnswered || hasWrongSelection
                ? 0
                : perChoiceMarks * selectedCorrectCount;
        }
        else
        {
            var normalizedSelectedAnswer = normalizedSelectedAnswers.FirstOrDefault();
            var normalizedCorrectAnswer = normalizedCorrectAnswers.FirstOrDefault();
            isCorrect = !string.IsNullOrEmpty(normalizedSelectedAnswer) &&
                        !string.IsNullOrEmpty(normalizedCorrectAnswer) &&
                        string.Equals(normalizedSelectedAnswer, normalizedCorrectAnswer, StringComparison.OrdinalIgnoreCase);
            marksAwarded = isCorrect
                ? question.Marks
                : assessment.NegativeMarking && hasAnswered
                    ? -Math.Abs(question.NegativeMarks)
                    : 0;
        }

        existingAnswer.SelectedAnswer = QuestionAnswerJsonHelper.SerializeAnswers(normalizedSelectedAnswers);
        existingAnswer.AnsweredAt = answeredAtUtc;
        existingAnswer.IsCorrect = isCorrect;
        existingAnswer.MarksAwarded = marksAwarded;

        return existingAnswer;
    }
}
