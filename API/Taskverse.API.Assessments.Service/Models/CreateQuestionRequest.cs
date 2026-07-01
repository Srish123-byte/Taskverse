using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Taskverse.API.Assessments.Service.Models;

public class CreateQuestionRequest
{
    [Required]
    public Guid CollegeId { get; set; }

    [Required]
    [MaxLength(200)]
    public string CreatedBy { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? RequesterRole { get; set; }

    [MaxLength(100)]
    public string? Stream { get; set; }

    public Guid? SubjectId { get; set; }

    [MaxLength(100)]
    public string? Subject { get; set; }

    public Guid? TopicId { get; set; }

    [MaxLength(200)]
    public string? Topic { get; set; }

    public List<string>? TopicTag { get; set; }

    [MaxLength(50)]
    public string? QuestionType { get; set; }

    public string? QuestionText { get; set; }

    public List<string>? Options { get; set; }

    public string? Answer { get; set; }

    public List<string>? CorrectAnswers { get; set; }

    [MaxLength(1000)]
    public string? Explanation { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Marks { get; set; }

    [Range(0, double.MaxValue)]
    public decimal NegativeMarks { get; set; }

    [Range(0, int.MaxValue)]
    public int DifficultyLevel { get; set; }

    [MaxLength(250)]
    public string? QuestionTitle { get; set; }

    public string? ProblemStatement { get; set; }

    public string? DetailedDescription { get; set; }

    public string? InputFormat { get; set; }

    public string? OutputFormat { get; set; }

    public string? ConstraintsText { get; set; }

    [MaxLength(50)]
    public string? DefaultLanguageCode { get; set; }

    [Range(1, int.MaxValue)]
    public int? DefaultTimeLimitMs { get; set; }

    [Range(1, int.MaxValue)]
    public int? DefaultMemoryLimitKb { get; set; }

    [Range(1, int.MaxValue)]
    public int? DefaultMaxCodeSizeKb { get; set; }

    public List<CodingQuestionExampleRequest>? Examples { get; set; }

    [JsonPropertyName("test_cases")]
    public List<CodingTestCaseRequest>? TestCases { get; set; }

    public int? SourceRowNumber { get; set; }
}

public class CodingQuestionExampleRequest
{
    public string? Input { get; set; }

    public string? Output { get; set; }

    public string? Explanation { get; set; }
}

public class CodingTestCaseRequest
{
    [MaxLength(30)]
    public string? InputFormat { get; set; }

    public string? InputData { get; set; }

    public string? ExpectedOutput { get; set; }

    public int ComparisonMode { get; set; } = 2;

    public decimal? NumericTolerance { get; set; }

    public bool IsHidden { get; set; }

    public bool IsSample { get; set; }

    [Range(1, int.MaxValue)]
    public int? TimeLimitMs { get; set; }

    [Range(1, int.MaxValue)]
    public int? MemoryLimitKb { get; set; }

    [JsonPropertyName("input_format")]
    public string? InputFormatAlias
    {
        set => InputFormat = value;
    }

    [JsonPropertyName("input_data")]
    public string? InputDataAlias
    {
        set => InputData = value;
    }

    [JsonPropertyName("expected_output")]
    public string? ExpectedOutputAlias
    {
        set => ExpectedOutput = value;
    }

    [JsonPropertyName("comparison_mode")]
    public int ComparisonModeAlias
    {
        set => ComparisonMode = value;
    }

    [JsonPropertyName("numeric_tolerance")]
    public decimal? NumericToleranceAlias
    {
        set => NumericTolerance = value;
    }

    [JsonPropertyName("is_hidden")]
    public bool IsHiddenAlias
    {
        set => IsHidden = value;
    }

    [JsonPropertyName("is_sample")]
    public bool IsSampleAlias
    {
        set => IsSample = value;
    }

    [JsonPropertyName("time_limit_ms")]
    public int? TimeLimitMsAlias
    {
        set => TimeLimitMs = value;
    }

    [JsonPropertyName("memory_limit_kb")]
    public int? MemoryLimitKbAlias
    {
        set => MemoryLimitKb = value;
    }
}

public class DeleteQuestionsRequest
{
    [Required]
    [MaxLength(200)]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string RequesterRole { get; set; } = string.Empty;

    [Required]
    public Guid CollegeId { get; set; }

    [Required]
    public List<Guid> QuestionIds { get; set; } = [];
}

public class QuestionBankSearchRequest
{
    [Required]
    public Guid CollegeId { get; set; }

    [Range(0, int.MaxValue)]
    public int? DifficultyLevel { get; set; }

    public Guid? SubjectId { get; set; }

    public Guid? TopicId { get; set; }

    [MaxLength(100)]
    public string? Subject { get; set; }

    [MaxLength(200)]
    public string? Topic { get; set; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 10;
}

public record QuestionTopicCatalogRecord(
    Guid TopicId,
    string TopicName);

public record QuestionSubjectCatalogRecord(
    Guid SubjectId,
    string SubjectName,
    List<QuestionTopicCatalogRecord> Topics);

public record QuestionClassificationCatalogRecord(
    List<QuestionSubjectCatalogRecord> Subjects);

public record CreateQuestionClassificationEntryRequest(
    Guid? SubjectId,
    string? SubjectName,
    string? TopicName);

public record QuestionClassificationEntryRecord(
    Guid SubjectId,
    string SubjectName,
    Guid? TopicId,
    string? TopicName);
