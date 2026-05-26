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

        existingAnswer.SelectedAnswer = NormalizeSelectedAnswer(request.SelectedAnswer);
        existingAnswer.AnsweredAt = answeredAtUtc;
        existingAnswer.IsCorrect = false;
        existingAnswer.MarksAwarded = 0;

        return existingAnswer;
    }

    private static string? NormalizeSelectedAnswer(string? selectedAnswer)
    {
        if (string.IsNullOrWhiteSpace(selectedAnswer))
        {
            return null;
        }

        return selectedAnswer.Trim();
    }
}
