namespace Taskverse.API.Assessments.Service.Models;

public record QuestionRecord(
    Guid QuestionId,
    Guid CollegeId,
    Guid? SubjectId,
    Guid? TopicId,
    string? Stream,
    string? Subject,
    string? Topic,
    List<string>? TopicTag,
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

public record PagedQuestionRecord(
    List<QuestionRecord> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
