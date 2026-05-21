using System.ComponentModel.DataAnnotations;

namespace Taskverse.API.Assessments.Service.Models;

public class CreateQuestionRequest
{
    [Required]
    public Guid CollegeId { get; set; }

    [Required]
    [MaxLength(200)]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Stream { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string TopicTag { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string QuestionType { get; set; } = string.Empty;

    [Required]
    public string QuestionText { get; set; } = string.Empty;

    public List<string>? Options { get; set; }

    [Required]
    public string Answer { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Explanation { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Marks { get; set; }

    [Range(0, double.MaxValue)]
    public decimal NegativeMarks { get; set; }

    [Range(0, int.MaxValue)]
    public int DifficultyLevel { get; set; }
}

public class DeleteQuestionsRequest
{
    [Required]
    [MaxLength(200)]
    public string CreatedBy { get; set; } = string.Empty;

    [Required]
    public List<Guid> QuestionIds { get; set; } = [];
}
