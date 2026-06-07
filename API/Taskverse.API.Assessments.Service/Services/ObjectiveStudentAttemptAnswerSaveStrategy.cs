using Microsoft.EntityFrameworkCore;
using Taskverse.API.Assessments.Service.Models;
using Taskverse.Data.DataAccess;

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

        var normalizedSelectedAnswer = NormalizeValue(request.SelectedAnswer);
        var normalizedCorrectAnswer = NormalizeValue(question.Answer);
        var isCorrect = !string.IsNullOrEmpty(normalizedSelectedAnswer) &&
                        !string.IsNullOrEmpty(normalizedCorrectAnswer) &&
                        string.Equals(normalizedSelectedAnswer, normalizedCorrectAnswer, StringComparison.OrdinalIgnoreCase);
        var hasAnswered = !string.IsNullOrEmpty(normalizedSelectedAnswer);
        var marksAwarded = isCorrect
            ? question.Marks
            : assessment.NegativeMarking && hasAnswered
                ? -Math.Abs(question.NegativeMarks)
                : 0;

        existingAnswer.SelectedAnswer = normalizedSelectedAnswer;
        existingAnswer.AnsweredAt = answeredAtUtc;
        existingAnswer.IsCorrect = isCorrect;
        existingAnswer.MarksAwarded = marksAwarded;

        return existingAnswer;
    }

    private static string? NormalizeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Join(
            " ",
            value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
