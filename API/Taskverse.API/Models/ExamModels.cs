using System.ComponentModel.DataAnnotations;

namespace Taskverse.Api.Models;

public class ExamResponseModel
{
    public string ExamId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalMarks { get; set; }
    public int PassingMarks { get; set; }
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateExamRequestModel
{
    [Required] public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required] public int DurationMinutes { get; set; }
    [Required] public int TotalMarks { get; set; }
    [Required] public int PassingMarks { get; set; }
    [Required] public string CreatedBy { get; set; } = string.Empty;
}

public class QuestionResponseModel
{
    public string QuestionId { get; set; } = string.Empty;
    public string ExamId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<string>? Options { get; set; }
    public int Marks { get; set; }
    public int Order { get; set; }
}

public class ExamSubmissionRequestModel
{
    [Required] public string ExamId { get; set; } = string.Empty;
    [Required] public string UserId { get; set; } = string.Empty;
    [Required] public List<AnswerRequestModel> Answers { get; set; } = new();
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public class AnswerRequestModel
{
    [Required] public string QuestionId { get; set; } = string.Empty;
    [Required] public string Answer { get; set; } = string.Empty;
}

public class ExamResultResponseModel
{
    public string SubmissionId { get; set; } = string.Empty;
    public string ExamId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int Score { get; set; }
    public int TotalMarks { get; set; }
    public bool IsPassed { get; set; }
    public DateTime CompletedAt { get; set; }
}
