using Taskverse.API.Assessments.Service.Models;
using Taskverse.Data.DataAccess;
using Taskverse.Data.Utilities;
using System.Text.Json;

namespace Taskverse.API.Assessments.Service.Mappings;

public static class QuestionMappings
{
    public static Question ToEntity(this CreateQuestionRequest request)
    {
        return new Question
        {
            CollegeId = request.CollegeId,
            SubjectId = request.SubjectId,
            Stream = request.Stream?.Trim(),
            Subject = request.Subject?.Trim(),
            TopicId = request.TopicId,
            Topic = request.Topic?.Trim(),
            TopicTag = request.TopicTag?.Trim(),
            QuestionType = request.QuestionType?.Trim() ?? string.Empty,
            QuestionText = request.QuestionText?.Trim() ?? string.Empty,
            Options = request.Options is null ? null : JsonSerializer.Serialize(request.Options),
            Answer = request.Answer?.Trim(),
            Explanation = request.Explanation?.Trim(),
            Marks = request.Marks,
            NegativeMarks = request.NegativeMarks,
            DifficultyLevel = request.DifficultyLevel,
            CreatedBy = request.CreatedBy
        };
    }

    public static QuestionRecord ToRecord(this Question question)
    {
        return new QuestionRecord(
            question.QuestionId,
            question.CollegeId,
            question.SubjectId,
            question.TopicId,
            question.Stream,
            question.Subject,
            question.Topic,
            question.TopicTag,
            question.QuestionType,
            question.QuestionText,
            DeserializeOptions(question.Options),
            question.Answer,
            question.Explanation,
            question.Marks,
            question.NegativeMarks,
            question.DifficultyLevel,
            question.Version,
            question.CreatedBy,
            UtcDateTime.Normalize(question.CreatedAt),
            UtcDateTime.Normalize(question.ModifiedAt));
    }

    public static void ApplyUpdates(this Question target, Question source)
    {
        target.CollegeId = source.CollegeId;
        target.SubjectId = source.SubjectId;
        target.Stream = source.Stream;
        target.Subject = source.Subject;
        target.TopicId = source.TopicId;
        target.Topic = source.Topic;
        target.TopicTag = source.TopicTag;
        target.QuestionType = source.QuestionType;
        target.QuestionText = source.QuestionText;
        target.Options = source.Options;
        target.Answer = source.Answer;
        target.Explanation = source.Explanation;
        target.Marks = source.Marks;
        target.NegativeMarks = source.NegativeMarks;
        target.DifficultyLevel = source.DifficultyLevel;
    }

    public static List<Guid> NormalizeQuestionIds(this IEnumerable<Guid> questionIds)
    {
        return questionIds
            .Where(questionId => questionId != Guid.Empty)
            .Distinct()
            .ToList();
    }

    private static List<string>? DeserializeOptions(string? options)
    {
        if (string.IsNullOrWhiteSpace(options))
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<string>>(options);
    }
}
