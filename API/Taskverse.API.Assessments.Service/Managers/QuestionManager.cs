using Microsoft.EntityFrameworkCore;
using Taskverse.Data.DataAccess;
using Taskverse.API.Assessments.Service.Mappings;

namespace Taskverse.API.Assessments.Service.Managers;

public class QuestionManager : IQuestionManager
{
    private static readonly HashSet<string> AllowedQuestionTypes =
    [
        "mcq",
        "fill in the blanks"
    ];

    private readonly TaskverseContext _context;

    public QuestionManager(TaskverseContext context)
    {
        _context = context;
    }

    public async Task<List<Question>> CreateQuestions(List<Question> questions)
    {
        if (questions.Count == 0)
        {
            throw new ArgumentException("At least one question is required.");
        }

        foreach (var question in questions)
        {
            ValidateQuestion(question);

            question.QuestionId = question.QuestionId == Guid.Empty ? Guid.NewGuid() : question.QuestionId;
        }

        _context.Questions.AddRange(questions);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Unable to save the question to the question bank.", ex);
        }

        return questions;
    }

    public async Task<Question> UpdateQuestion(Guid questionId, Question updatedQuestion)
    {
        ValidateQuestion(updatedQuestion);

        var existingQuestion = await _context.Questions.FirstOrDefaultAsync(question => question.QuestionId == questionId);
        if (existingQuestion is null)
        {
            throw new KeyNotFoundException($"Question with id '{questionId}' was not found.");
        }

        if (!string.Equals(existingQuestion.CreatedBy?.Trim(), updatedQuestion.CreatedBy?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Only the user who created this question can update it.");
        }

        existingQuestion.ApplyUpdates(updatedQuestion);
        existingQuestion.Version += 1;
        existingQuestion.ModifiedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Unable to update the question in the question bank.", ex);
        }

        return existingQuestion;
    }

    public async Task<List<Guid>> DeleteQuestions(string createdBy, List<Guid> questionIds)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
        {
            throw new ArgumentException("CreatedBy is required.");
        }

        var normalizedQuestionIds = questionIds.NormalizeQuestionIds();
        if (normalizedQuestionIds.Count == 0)
        {
            throw new ArgumentException("At least one valid question id is required.");
        }

        var questions = await _context.Questions
            .Where(question => normalizedQuestionIds.Contains(question.QuestionId))
            .ToListAsync();

        var missingQuestionIds = normalizedQuestionIds.Except(questions.Select(question => question.QuestionId)).ToList();
        if (missingQuestionIds.Count > 0)
        {
            throw new KeyNotFoundException($"Question(s) not found: {string.Join(", ", missingQuestionIds)}.");
        }

        var unauthorizedQuestion = questions.FirstOrDefault(question =>
            !string.Equals(question.CreatedBy?.Trim(), createdBy.Trim(), StringComparison.OrdinalIgnoreCase));
        if (unauthorizedQuestion is not null)
        {
            throw new UnauthorizedAccessException("Only the user who created a question can delete it.");
        }

        var linkedQuestionIds = questions
            .Where(question => question.AssessmentId.HasValue)
            .Select(question => question.QuestionId)
            .ToList();

        if (linkedQuestionIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Question(s) cannot be deleted because they are attached to an assessment: {string.Join(", ", linkedQuestionIds)}.");
        }

        _context.Questions.RemoveRange(questions);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException("Unable to delete the question(s) from the question bank.", ex);
        }

        return questions.Select(question => question.QuestionId).ToList();
    }

    private static void ValidateQuestion(Question question)
    {
        if (question.CollegeId == Guid.Empty)
        {
            throw new ArgumentException("CollegeId is required.");
        }

        if (string.IsNullOrWhiteSpace(question.CreatedBy))
        {
            throw new ArgumentException("CreatedBy is required.");
        }

        if (string.IsNullOrWhiteSpace(question.Stream))
        {
            throw new ArgumentException("Stream is required.");
        }

        if (string.IsNullOrWhiteSpace(question.Subject))
        {
            throw new ArgumentException("Subject is required.");
        }

        if (string.IsNullOrWhiteSpace(question.Topic))
        {
            throw new ArgumentException("Topic is required.");
        }

        if (string.IsNullOrWhiteSpace(question.TopicTag))
        {
            throw new ArgumentException("Topic tag is required.");
        }

        if (string.IsNullOrWhiteSpace(question.QuestionType))
        {
            throw new ArgumentException("Question type is required.");
        }

        var normalizedQuestionType = question.QuestionType.Trim().ToLowerInvariant();
        if (!AllowedQuestionTypes.Contains(normalizedQuestionType))
        {
            throw new ArgumentException("Question type must be either 'mcq' or 'fill in the blanks'.");
        }

        question.QuestionType = normalizedQuestionType;

        if (string.IsNullOrWhiteSpace(question.QuestionText))
        {
            throw new ArgumentException("Question text is required.");
        }

        if (normalizedQuestionType == "mcq" && string.IsNullOrWhiteSpace(question.Options))
        {
            throw new ArgumentException("Options are required for mcq questions.");
        }

        if (string.IsNullOrWhiteSpace(question.Answer))
        {
            throw new ArgumentException("Answer is required.");
        }

        if (question.Marks < 0)
        {
            throw new ArgumentException("Marks cannot be negative.");
        }

        if (question.NegativeMarks < 0)
        {
            throw new ArgumentException("Negative marks cannot be negative.");
        }
    }
}
