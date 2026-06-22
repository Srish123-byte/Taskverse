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
    string? QuestionType,
    string? QuestionText,
    List<string>? Options,
    string? Answer,
    string? Explanation,
    decimal Marks,
    decimal NegativeMarks,
    int DifficultyLevel,
    string? QuestionTitle,
    string? ProblemStatement,
    string? DetailedDescription,
    string? InputFormat,
    string? OutputFormat,
    string? ConstraintsText,
    List<CodingQuestionExampleRecord>? Examples,
    string? DefaultLanguageCode,
    int? DefaultTimeLimitMs,
    int? DefaultMemoryLimitKb,
    int? DefaultMaxCodeSizeKb,
    List<CodingTestCaseRecord>? TestCases,
    int Version,
    string CreatedBy,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

public record CodingQuestionExampleRecord(
    string? Input,
    string? Output,
    string? Explanation);

public record CodingTestCaseRecord(
    Guid TestCaseId,
    string? InputFormat,
    string? InputData,
    string? ExpectedOutput,
    int ComparisonMode,
    decimal? NumericTolerance,
    bool IsHidden,
    bool IsSample,
    int? TimeLimitMs,
    int? MemoryLimitKb);

public record PagedQuestionRecord(
    List<QuestionRecord> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
