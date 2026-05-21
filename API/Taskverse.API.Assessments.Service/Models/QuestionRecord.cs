namespace Taskverse.API.Assessments.Service.Models;

public record QuestionRecord(
    Guid QuestionId,
    Guid CollegeId,
    string? Stream,
    string? Subject,
    string? Topic,
    string? TopicTag,
    string QuestionType,
    string QuestionText,
    List<string>? Options,
    string? Answer,
    string? Explanation,
    decimal Marks,
    decimal NegativeMarks,
    int DifficultyLevel,
    int Version,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? ModifiedAt);
