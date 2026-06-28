using System.ComponentModel.DataAnnotations;

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

    [Required]
    [MaxLength(100)]
    public string Stream { get; set; } = string.Empty;

    public Guid? SubjectId { get; set; }

    [MaxLength(100)]
    public string? Subject { get; set; }

    public Guid? TopicId { get; set; }

    [MaxLength(200)]
    public string? Topic { get; set; }

    [Required]
    public List<string> TopicTag { get; set; } = [];

    [Required]
    [MaxLength(50)]
    public string QuestionType { get; set; } = string.Empty;

    [Required]
    public string QuestionText { get; set; } = string.Empty;

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

    public int? SourceRowNumber { get; set; }
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
